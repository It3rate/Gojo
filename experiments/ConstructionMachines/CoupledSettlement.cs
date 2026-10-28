using Gojo.Core;
using Gojo.Encoding;
using Gojo.Equations;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>
/// A selected local experiment: outline placement creates attachment demands;
/// observed joint mismatch can authorize bounded, private proposal reviews.
/// The controller schedules no replacement by pass number or concept name.
/// </summary>
public sealed class CoupledSettlement
{
    // G(world, joint, context, policy, proposals, selected proposal, load,
    // clock, timeline, last read, last decision, goal links, attachment links).
    // context = G(coordinate basis, interpolation grade, norm axis, norm calibration).
    // policy = G(load threshold, minimum benefit, review-budget span,
    //            attempts carrier, preview-pass span).
    public NegotiationWorld World { get; }
    public JointAssembly Joint { get; }
    public Relation Context { get; }
    public Relation Policy { get; }
    public Relation Proposals { get; }
    public IElement Selected { get; private set; } = new Placeholder();
    public MismatchLoad Load { get; }
    public Relation Timeline { get; private set; }
    public Relation LastRead { get; private set; }
    public IElement LastDecision { get; private set; } = new Placeholder();
    public Relation Surface => G(World.Surface, Joint.Surface, Context, Policy, Proposals, Selected,
        Load.Surface, clock, Timeline, LastRead, LastDecision, G(goalLinks.Select(l => (IElement)l.Surface).ToArray()),
        G(attachmentLinks.Select(l => (IElement)l.Surface).ToArray()));
    public NegotiatingMachine Outline => World.Machines[0];
    private readonly NegotiationLink[] goalLinks, attachmentLinks;
    private readonly Carrier clock;
    private readonly IElement initial;
    private readonly List<Relation> steps = [];
    private Relation Basis => (Relation)F(Context)[0];
    private Carrier Grade => (Carrier)F(Context)[1];
    private Carrier NormAxis => (Carrier)F(Context)[2];
    private Carrier FeatureAxis { get { TimeStepSequence.TryReadSpan((Relation)F(Basis)[1], out var p, out _); return p.Carrier; } }

    private CoupledSettlement(NegotiationWorld world, JointAssembly joint, Relation context, Relation policy,
        Relation proposals, MismatchLoad load, NegotiationLink[] goals, NegotiationLink[] attachments)
    {
        World = world; Joint = joint; Context = context; Policy = policy; Proposals = proposals; Load = load;
        goalLinks = goals; attachmentLinks = attachments; clock = new Carrier();
        LastRead = Observe(); initial = LastRead; Timeline = TimeStepSequence.Create(new Carrier(), clock, initial);
    }
    private CoupledSettlement(Relation surface)
    {
        var f = F(surface);
        if (f.Length != 13) throw new ArgumentException("An exact coupled-settlement surface is required.");
        World = NegotiationWorld.Restore((Relation)f[0]); Joint = JointAssembly.Restore((Relation)f[1], World.Machines[1]);
        Context = (Relation)f[2]; Policy = (Relation)f[3]; Proposals = (Relation)f[4]; Selected = f[5]; Load = MismatchLoad.Restore((Relation)f[6]);
        clock = (Carrier)f[7]; Timeline = (Relation)f[8]; LastRead = (Relation)f[9]; LastDecision = f[10];
        goalLinks = F(f[11]).Cast<Relation>().Select(NegotiationLink.Restore).ToArray();
        attachmentLinks = F(f[12]).Cast<Relation>().Select(NegotiationLink.Restore).ToArray();
        if (!TimeStepSequence.TryRead(Timeline, out _, out var start, out var retained)) throw new ArgumentException("The coupled timeline is unavailable.");
        initial = start; steps.AddRange(retained);
    }
    public static CoupledSettlement Restore(Relation surface) => new(surface);

    public static CoupledSettlement Create(long targetRise = 40, long shapeStep = 4, long reviewBudget = 3,
        bool suitableProposal = true, long threshold = 12, long previewPasses = 10)
    {
        var r = new CoupledRecipes();
        var outline = new NegotiatingMachine(r.Outline(r.ShapeX, r.Width, r.Height, r.RoofRise)); r.BindOutline(outline, shapeStep);
        var joint = new JointAssembly(r.Joint(), G(new Relation(r.External[2], r.External[3])));
        CoupledRecipes.BindRules(joint, r.JointRules());
        var goalDefinition = r.Outline(Program.Parameter(80), Program.Parameter(160), Program.Parameter(120), Program.Parameter(targetRise));
        var goal = new NegotiatingMachine((Relation)Program.CaptureObservation(goalDefinition));
        var goals = new[] { (0, 0), (1, 0), (3, 1), (6, 1) }
            .Select(p => new NegotiationLink(outline.Port(p.Item1, p.Item2), goal.Port(p.Item1, p.Item2))).ToArray();
        var shapePorts = new[] { 3, 2, 5, 4 };
        var attachments = Enumerable.Range(0, 4).SelectMany(i => Enumerable.Range(0, 2)
            .Select(axis => new NegotiationLink(outline.Port(shapePorts[i], axis), joint.Mechanical.Port(i, axis)))).ToArray();
        var world = new NegotiationWorld([outline, joint.Mechanical, goal], [.. goals, .. attachments]);
        var norm = new Carrier(r.Space.Y.UnitShape);
        var context = G(r.Space.Basis, r.Space.Grade, norm, G(
            new Relation(Relation.Segment(r.Space.X, 0, 1), Relation.Segment(norm, 0, 1)),
            new Relation(Relation.Segment(r.Space.Y, 0, 1), Relation.Segment(norm, 0, 1))));
        var load = new MismatchLoad(r.Space.Y); var attempts = new Carrier();
        var policy = G(new CarrierLocation(load.Remaining, threshold), new CarrierLocation(r.Space.Y, 4),
            Relation.Segment(attempts, 0, reviewBudget), attempts, Relation.Segment(new Carrier(), 0, previewPasses));
        var proposals = suitableProposal ? G(r.Proposal(1, true), r.Proposal(-1, false), r.Proposal(-1, true))
            : G(r.Proposal(1, true), r.Proposal(-1, false));
        return new CoupledSettlement(world, joint, context, policy, proposals, load, goals, attachments);
    }

    // Observation fields: goal norm read, attachment norm read, feature-error
    // equation read, neighbour feature read, joint feature read, projected
    // neighbour snapshot, open-path count. Reading never introduces load.
    public Relation Observe()
    {
        var goals = goalLinks.Select(l => l.Plan(Outline.Publish(l.Left), World.Machines[2].Publish(l.Right))).ToArray();
        var attachments = attachmentLinks.Select(l => l.Plan(Outline.Publish(l.Left), Joint.Mechanical.Publish(l.Right))).ToArray();
        var snapshot = ProjectNeighbour(attachments, Joint.Mechanical);
        var neighbour = Feature((Relation)F(snapshot)[2]);
        var own = Feature(CapturePoints(Joint.Mechanical));
        var difference = Equation.PredictWitness(new Carrier(), Equation.Difference(FeatureAxis, FeatureAxis, FeatureAxis),
            new Relation(F(own)[2], F(neighbour)[2]), FeatureAxis);
        var open = F(F(Joint.CurrentRead)[2])[2];
        var openCount = F(open).Count(p => F(p)[2] is Placeholder);
        return ReadWitness.Create(new Carrier(), G(Outline.CurrentPublication, Joint.Mechanical.CurrentPublication, World.Machines[2].CurrentPublication),
            G(Context, G(goalLinks.Select(l => (IElement)l.Surface).ToArray()), G(attachmentLinks.Select(l => (IElement)l.Surface).ToArray())),
            G(Norm(goals), Norm(attachments), difference, neighbour, own, snapshot, new CarrierLocation(NormAxis, openCount)),
            G(goals.Select(NegotiationLink.Residual).Concat(attachments.Select(NegotiationLink.Residual)).ToArray()), G(Joint.CurrentRead));
    }

    public sealed record Transition(Relation Native, Relation AfterMotion, Relation AfterDecision, Relation LoadReceipt,
        Relation Decision, IElement PreviousSelection, bool Rewritten);

    public Transition Step()
    {
        var before = LastRead; var previous = Selected;
        var motion = Joint.Step(World); var afterMotion = Observe();
        var signed = FeatureMismatch(afterMotion);
        if (!Load.TrySample(signed, motion, afterMotion, out var ledger)) throw new InvalidOperationException("A fresh numerical transition was unexpectedly consumed.");
        var decision = Review(afterMotion, out var rewritten);
        // No new mechanical publication means the same qualified observation
        // is still current. Keep its identity instead of recalculating it.
        LastDecision = decision; LastRead = rewritten ? Observe() : afterMotion;
        var step = TimeStepSequence.Step(new Carrier(), clock, (long)clock.Value, checked((long)clock.Value + 1), before,
            G(motion, ledger, decision, previous, Selected), LastRead,
            G(FeatureMismatch(LastRead), NormValue(F(F(LastRead)[2])[0]), NormValue(F(F(LastRead)[2])[1])));
        clock.Add(1); steps.Add(step); Timeline = TimeStepSequence.Create(new Carrier(), clock, initial, steps.ToArray());
        return new Transition(step, afterMotion, LastRead, ledger, decision, previous, rewritten);
    }

    private Relation Review(Relation observation, out bool rewritten)
    {
        rewritten = false; var p = F(Policy); var attempts = (Carrier)p[3];
        var gate = OperationClaim.AtLeast(new Carrier(), new CarrierLocation(Load.Remaining, (long)Load.Remaining.Value), p[0], new Carrier(Load.Remaining.UnitShape));
        TimeStepSequence.TryReadSpan((Relation)p[2], out _, out var ceiling);
        var budget = OperationClaim.AtMost(new Carrier(), new CarrierLocation(attempts, checked((long)attempts.Value + 1)),
            ceiling, new Carrier(attempts.UnitShape));
        if (Selected is not Placeholder || !Quiet(gate) || !Quiet(budget))
            return ReadWitness.Create(new Carrier(), observation, G(gate, budget), new Placeholder(), G(gate, budget), G(Selected));
        attempts.Add(1);
        var reviews = F(Proposals).Cast<Relation>().Select(proposal => Probe(proposal, observation)).ToArray();
        var eligible = reviews.Where(Eligible).ToArray();
        if (eligible.Length == 0)
            return ReadWitness.Create(new Carrier(), observation, G(gate, budget), G(new Placeholder(), G(reviews)), G(reviews), G(Policy));
        var selectedReview = eligible.Aggregate((best, candidate) => Fraction.Read(F(F(candidate)[2])[2]) > Fraction.Read(F(F(best)[2])[2]) ? candidate : best);
        var proposal = (Relation)F(selectedReview)[1]; var f = F(proposal);
        if (!Joint.TryRewrite((Relation)f[0], (Relation)f[1], (Relation)f[2], out var applied))
            return ReadWitness.Create(new Carrier(), observation, G(gate, budget), new Placeholder(), G(applied, selectedReview), G(reviews));
        var binding = CoupledRecipes.BindRules(Joint, (Relation)f[3]); Selected = proposal; rewritten = true;
        return ReadWitness.Create(new Carrier(), observation, G(gate, budget), G(proposal, G(reviews)), G(), G(applied, binding, selectedReview));
    }

    // Probe result = G(before feature error, after feature error, improvement,
    // before endpoint norm, after endpoint norm, before open paths, after open
    // paths, G(min-improvement claim, endpoint claim, path claim)).
    public Relation Probe(Relation proposal, Relation? observation = null)
    {
        observation ??= Observe(); var of = F(F(observation)[2]); var targetPoints = (Relation)F(of[5])[2];
        // Deliberate bounded fork of only this local construction, candidate and
        // captured neighbour coordinates. No neighbour's private model or goal.
        var focus = (Relation)Program.CaptureObservation(G(Joint.Definition, proposal, targetPoints, Context, Joint.RequestedPaths, Policy));
        var f = F(focus); var local = new JointAssembly((Relation)f[0], (Relation)f[4]); var candidate = F(f[1]);
        var beforePaths = OpenPaths(local);
        if (!local.TryRewrite((Relation)candidate[0], (Relation)candidate[1], (Relation)candidate[2], out var replacement))
            return ReadWitness.Create(new Carrier(), focus, proposal, new Placeholder(), G(replacement), G());
        var routes = CoupledRecipes.BindRules(local, (Relation)candidate[3]);
        var context = F(f[3]); var basis = (Relation)context[0]; var grade = (Carrier)context[1]; var norm = (Carrier)context[2];
        TimeStepSequence.TryReadSpan((Relation)F(basis)[1], out var featureOrigin, out _); var feature = featureOrigin.Carrier;
        var target = new NegotiatingMachine(TransformSurface.Create(new Carrier(), G(), f[2],
            G(F(f[2]).Select(_ => (IElement)new Placeholder()).ToArray()), G(), G(basis, f[2])));
        var links = Enumerable.Range(0, 4).SelectMany(i => Enumerable.Range(0, 2).Select(axis => new NegotiationLink(local.Mechanical.Port(i, axis), target.Port(i, axis)))).ToArray();
        var world = new NegotiationWorld([local.Mechanical, target], links);
        var featureReads = new List<IElement>(); var normReads = new List<IElement>();
        var beforeFeature = Error(local.Mechanical); var beforeNorm = LocalNorm();
        var budget = (Relation)F(f[5])[4]; TimeStepSequence.TryReadSpan(budget, out var from, out var to);
        for (var pass = from.Ticks; pass < to.Ticks; pass++) local.Step(world);
        var afterFeature = Error(local.Mechanical); var afterNorm = LocalNorm(); var afterPaths = OpenPaths(local);
        var improvementRead = Equation.PredictWitness(new Carrier(), Equation.Difference(feature, feature, feature),
            new Relation(afterFeature.Encode(feature), beforeFeature.Encode(feature)), feature);
        var improvement = Fraction.Read(F(improvementRead)[2]);
        // Comparisons are performed in the original local publication bases.
        // The explicit fork correspondence below accounts for restored axes.
        var benefitClaim = ClaimAtLeast(improvement, Fraction.Read(F(Policy)[1]), FeatureAxis);
        var endpointClaim = ClaimAtMost(afterNorm, beforeNorm, NormAxis);
        var pathClaim = ClaimAtMost(new Fraction(afterPaths), new Fraction(beforePaths), NormAxis);
        var result = G(beforeFeature.Encode(FeatureAxis), afterFeature.Encode(FeatureAxis), improvement.Encode(FeatureAxis),
            beforeNorm.Encode(NormAxis), afterNorm.Encode(NormAxis), new CarrierLocation(NormAxis, beforePaths), new CarrierLocation(NormAxis, afterPaths),
            G(benefitClaim, endpointClaim, pathClaim));
        var correspondence = G(new Relation(Relation.Segment(feature, 0, 1), Relation.Segment(FeatureAxis, 0, 1)),
            new Relation(Relation.Segment(norm, 0, 1), Relation.Segment(NormAxis, 0, 1)));
        return ReadWitness.Create(new Carrier(), G(Joint.Definition, of[5]), proposal, result,
            EligibleClaims(F(result)[7]) ? G() : G(benefitClaim, endpointClaim, pathClaim),
            G(focus, replacement, routes, world.Surface, local.Surface, budget, correspondence,
                G(featureReads.ToArray()), G(normReads.ToArray()), improvementRead));

        Fraction Error(NegotiatingMachine machine)
        {
            var desired = Feature((Relation)f[2], basis, grade);
            var own = Feature(CapturePoints(machine), basis, grade);
            var difference = Equation.PredictWitness(new Carrier(), Equation.Difference(feature, feature, feature),
                new Relation(F(own)[2], F(desired)[2]), feature);
            var zeroClaim = OperationClaim.Equality(new Carrier(), F(difference)[2], new CarrierLocation(feature, 0), feature);
            OperationClaim.TryRead(zeroClaim, out _, out _, out _, out var magnitude, out _, out _);
            featureReads.Add(G(desired, own, difference, zeroClaim));
            return Fraction.Read(magnitude);
        }
        Fraction LocalNorm()
        {
            var read = Norm(world.Compare(), norm, context[3]); normReads.Add(read);
            return Fraction.Read(F(read)[2]);
        }
    }

    public static bool Eligible(Relation review) => F(review)[2] is Relation result && result.TryReadOrderedGroup(out var f) && f.Length == 8 && EligibleClaims(f[7]);
    private static bool EligibleClaims(IElement group) => F(group) is { Length: 3 } claims && claims.All(c => c is Relation claim && Quiet(claim));
    private static bool Quiet(Relation claim) => OperationClaim.TryReadResolvedViolation(claim, out var violation) && violation.Ticks == 0;
    private static Relation ClaimAtLeast(Fraction a, Fraction b, Carrier axis) => OperationClaim.AtLeast(new Carrier(), a.Encode(axis), b.Encode(axis), new Carrier(axis.UnitShape));
    private static Relation ClaimAtMost(Fraction a, Fraction b, Carrier axis) => OperationClaim.AtMost(new Carrier(), a.Encode(axis), b.Encode(axis), new Carrier(axis.UnitShape));
    public static IElement FeatureMismatch(Relation read) => NormValue(F(F(read)[2])[2]);
    public static IElement NormValue(IElement read) => F(read)[2];
    public static double Number(IElement scalar) { var n = Fraction.Read(scalar); return (double)n.N / (double)n.D; }
    public static int OpenPaths(JointAssembly joint) => F(F(F(joint.CurrentRead)[2])[2]).Count(p => F(p)[2] is Placeholder);

    private Relation Norm(Relation[] plans) => Norm(plans, NormAxis, F(Context)[3]);
    private static Relation Norm(Relation[] plans, Carrier normAxis, IElement normSurface)
    {
        IElement sum = new CarrierLocation(normAxis, 0); var terms = new List<IElement>();
        foreach (var plan in plans)
        {
            var value = NegotiationLink.Residual(plan); ProjectionResidue.TryRead(value, out var axis, out _, out _);
            var map = (Relation)F(F(plan)[1])[2];
            TimeStepSequence.TryReadSpan((Relation)map.Left, out var leftStart, out var leftEnd);
            TimeStepSequence.TryReadSpan((Relation)map.Right, out var rightStart, out var rightEnd);
            var leftExtent = checked(leftEnd.Ticks - leftStart.Ticks); var rightExtent = checked(rightEnd.Ticks - rightStart.Ticks);
            var calibration = new Relation(Relation.Segment(axis, 0, rightExtent), Relation.Segment(normAxis, 0, leftExtent));
            var projected = (Fraction.Read(value) * new Fraction(leftExtent, rightExtent)).Encode(normAxis);
            var projection = ReadWitness.Create(new Carrier(), value, calibration, projected, G(), plan);
            var zero = OperationClaim.Equality(new Carrier(), projected, new CarrierLocation(normAxis, 0), normAxis);
            if (!OperationClaim.TryRead(zero, out _, out _, out _, out var magnitude, out _, out _))
                throw new InvalidDataException("The selected endpoint magnitude is unresolved.");
            var addition = Equation.PredictWitness(new Carrier(), Equation.Sum(normAxis, Equation.UnitPort(normAxis), Equation.UnitPort(normAxis)),
                new Relation(sum, magnitude), normAxis);
            sum = F(addition)[2]; terms.Add(G(projection, zero, addition));
        }
        return ReadWitness.Create(new Carrier(), G(plans), normSurface, sum, G(), G(terms.ToArray()));
    }

    private Relation ProjectNeighbour(Relation[] plans, NegotiatingMachine local)
    {
        var points = new List<IElement>(); var witnesses = new List<IElement>();
        for (var i = 0; i < 4; i++)
        {
            var coordinates = new List<IElement>();
            for (var axis = 0; axis < 2; axis++)
            {
                var plan = plans[i * 2 + axis]; var request = F(NegotiationLink.Requests(plan)[1]); var own = F(request[1]);
                var basis = NegotiatingMachine.PortAxis(local.Port(i, axis));
                var target = (Fraction.Read(own[2]) + Fraction.Read(request[3])).Encode(basis);
                coordinates.Add(target);
                witnesses.Add(ReadWitness.Create(new Carrier(), G(request[1], request[2]), F(plan)[1], target, G(), plan));
            }
            points.Add(Relation.OrderedGroup((Carrier)local.Point(i).Right, coordinates.ToArray()));
        }
        return ReadWitness.Create(new Carrier(), G(plans), G(local.Ports.ToArray()), G(points.ToArray()), G(), G(witnesses.ToArray()));
    }
    private Relation Feature(Relation points) => Feature(points, Basis, Grade);
    private static Relation Feature(Relation points, Relation basis, Carrier grade)
    {
        var p = F(points); var trace = new List<Relation>();
        var midpoint = Evaluate(Interpolate(basis, p[0], p[1], new CarrierLocation(grade, 1), 2, grade), [], trace);
        if (!AffineFrame.TryDifference(G(midpoint, p[3], basis), out var difference))
            throw new InvalidDataException("The selected joint differential remains unresolved.");
        var value = F(F(difference)[2])[1];
        return ReadWitness.Create(new Carrier(), points, G(basis, new Relation(midpoint, p[3])), value, F(difference)[3], G(G(trace.ToArray()), difference));
    }
    private static Relation CapturePoints(NegotiatingMachine machine) => G(machine.Points.Select(point =>
        (IElement)Relation.OrderedGroup((Carrier)point.Right, F(point).Select(c =>
        { ProjectionResidue.TryRead(c, out var axis, out var n, out var d); return ProjectionResidue.Create(axis, n, d); }).ToArray())).ToArray());
}
