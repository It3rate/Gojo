using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>
/// A caller-owned captured-round schedule. Error participants read public
/// samples, make synthetic requests and observe their effects. Only a receiver
/// admits changes to its private construction. No learned model is required.
/// </summary>
public sealed class EnsembleSettlement
{
    // G(machines, joint, obligations, selected obligation participation, score
    // axis, review policy, clock, timeline, last read, previous feature samples,
    // selected replacement, persistent-demand count, review count, proposals,
    // last review, last pressure). Labels and display coordinates are derived.
    public NegotiatingMachine[] Machines { get; }
    public JointAssembly Joint { get; }
    public EvidenceObligation[] Obligations { get; }
    public Relation Participation { get; }
    public Carrier ScoreAxis { get; }
    public Relation Policy { get; }
    public Relation Timeline { get; private set; }
    public Relation LastRead { get; private set; }
    public Relation Memory { get; private set; } = G();
    public IElement Selected { get; private set; } = new Placeholder();
    public Carrier Persistence { get; }
    public Carrier Reviews { get; }
    public Relation Proposals { get; }
    public IElement LastReview { get; private set; } = new Placeholder();
    public IElement LastPressure { get; private set; } = new Placeholder();
    public Relation Surface => G(G(Machines.Select(m => (IElement)m.Surface).ToArray()), Joint.Surface,
        G(Obligations.Select(o => (IElement)o.Surface).ToArray()), Participation, ScoreAxis, Policy, clock, Timeline,
        LastRead, Memory, Selected, Persistence, Reviews, Proposals, LastReview, LastPressure);
    public EvidenceObligation[] Active => F(Participation).Select(s => Obligations.Single(o => ReferenceEquals(o.Surface, s))).ToArray();
    private readonly Carrier clock;
    private readonly IElement initial;
    private readonly List<Relation> steps = [];

    public EnsembleSettlement(NegotiatingMachine[] machines, JointAssembly joint, EvidenceObligation[] obligations,
        Relation participation, Relation proposals, long reviewBudget = 2, long previewPasses = 8, Relation? negotiationPassages = null)
    {
        Machines = machines; Joint = joint; Obligations = obligations; Participation = participation; Proposals = proposals;
        ScoreAxis = new Carrier(); clock = new Carrier(); Persistence = new Carrier(); Reviews = new Carrier();
        Policy = G(new CarrierLocation(ScoreAxis, 6), new CarrierLocation(Persistence, 3),
            Relation.Segment(Reviews, 0, reviewBudget), Relation.Segment(new Carrier(), 0, previewPasses),
            new CarrierLocation(ScoreAxis, 3), G(joint.Mechanical.Port(0, 1), joint.Mechanical.Port(1, 1), joint.Mechanical.Port(3, 1)), negotiationPassages ?? G());
        LastRead = Observe(Capture()); initial = LastRead; Timeline = TimeStepSequence.Create(new Carrier(), clock, initial);
    }
    private EnsembleSettlement(Relation surface)
    {
        var f = F(surface); if (f.Length != 16) throw new ArgumentException("An exact ensemble settlement surface is required.");
        Machines = F(f[0]).Cast<Relation>().Select(NegotiatingMachine.Restore).ToArray();
        Joint = JointAssembly.Restore((Relation)f[1], Machines[1]);
        Obligations = F(f[2]).Cast<Relation>().Select(EvidenceObligation.Restore).ToArray(); Participation = (Relation)f[3];
        ScoreAxis = (Carrier)f[4]; Policy = (Relation)f[5]; clock = (Carrier)f[6]; Timeline = (Relation)f[7]; LastRead = (Relation)f[8];
        Memory = (Relation)f[9]; Selected = f[10]; Persistence = (Carrier)f[11]; Reviews = (Carrier)f[12]; Proposals = (Relation)f[13];
        LastReview = f[14]; LastPressure = f[15];
        if (!TimeStepSequence.TryRead(Timeline, out _, out var start, out var retained)) throw new ArgumentException("The ensemble timeline is unavailable.");
        initial = start; steps.AddRange(retained);
    }
    public static EnsembleSettlement Restore(Relation surface) => new(surface);
    public Dictionary<Relation, Relation> Capture() => Machines.SelectMany(m => F(m.CurrentPublication).Cast<Relation>())
        .ToDictionary(p => (Relation)F(p)[0], p => p);

    // Result = G(G(assessment witnesses), calibrated known-demand sum,
    // unresolved count, required-path count). Unknown terms remain in residue.
    public Relation Observe(IReadOnlyDictionary<Relation, Relation> samples)
    {
        var reads = Active.Select(o => o.Assess(samples)).ToArray();
        IElement sum = new CarrierLocation(ScoreAxis, 0); var trace = new List<IElement>(); var pending = new List<IElement>();
        var support = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var pair in Active.Zip(reads))
        {
            // The audit counts a declared support correspondence once even in
            // the deliberately unpooled-response control.
            if (!support.Add(pair.First.Support)) continue;
            if (!EvidenceObligation.Resolved(pair.Second)) { pending.Add(pair.Second); continue; }
            var influence = Fraction.Read(F(pair.First.Surface)[4]);
            var map = Equation.Affine(pair.First.Axis, 0, (long)influence.D, ScoreAxis, 0, (long)influence.N);
            var converted = Equation.PredictWitness(new Carrier(), map, EvidenceObligation.Demand(pair.Second), ScoreAxis);
            var addition = Equation.PredictWitness(new Carrier(), Equation.Sum(ScoreAxis, Equation.UnitPort(ScoreAxis), Equation.UnitPort(ScoreAxis)),
                new Relation(sum, F(converted)[2]), ScoreAxis);
            sum = F(addition)[2]; trace.Add(G(converted, addition));
        }
        return ReadWitness.Create(new Carrier(), G(samples.Values.Cast<IElement>().ToArray()), G(Participation, ScoreAxis),
            G(G(reads), sum, new CarrierLocation(ScoreAxis, pending.Count), new CarrierLocation(ScoreAxis, CoupledSettlement.OpenPaths(Joint))),
            G(pending.ToArray()), G(trace.ToArray()));
    }
    public static IElement Cost(Relation read) => F(F(read)[2])[1];
    public static int Unknown(Relation read) => (int)Fraction.Read(F(F(read)[2])[2]).N;
    public static Relation[] Assessments(Relation read) => F(F(F(read)[2])[0]).Cast<Relation>().ToArray();

    public sealed record Transition(Relation Native, Relation Before, Relation AfterMotion, Relation AfterDecision,
        Relation Plans, Relation Negotiation, Relation Motion, Relation Effects, IElement Review, bool Rewritten);

    public Transition Step()
    {
        var samples = Capture(); var before = Observe(samples);
        var occasion = G(new CarrierLocation(clock, (long)clock.Value), G(samples.Values.Cast<IElement>().ToArray()));
        var plans = Plans(samples, before, Memory, occasion);
        var negotiated = Negotiate(plans.SelectMany(EvidenceObligation.Requests).ToArray());
        var requests = F(F(negotiated)[2]).Cast<Relation>().ToArray(); var receipts = new List<IElement>();
        foreach (var machine in Machines)
        {
            var addressed = requests.Where(r => machine.Ports.Any(p => ReferenceEquals(p, F(r)[0]))).ToArray();
            if (addressed.Length > 0)
            {
                if (!machine.Apply(addressed, out var receipt)) throw new InvalidOperationException("A captured ensemble request was not admitted as a current batch.");
                receipts.Add(receipt);
            }
        }
        var motion = G(receipts.ToArray()); Joint.ObserveTransition(motion);
        var afterMotion = Observe(Capture()); var effects = G(Active.Zip(Assessments(before), (o, r) => (o, r))
            .Zip(Assessments(afterMotion), (pair, after) => (IElement)pair.o.Response(pair.r, after, motion)).ToArray());
        Memory = G(Active.Zip(Assessments(before), (o, read) => (IElement)new Relation(o.Surface,
            EvidenceObligation.Resolved(read) ? EvidenceObligation.Value(read) : new Placeholder())).ToArray());
        // A second query estimates continuing, opposed public requests AFTER
        // response. This query does not transfer change or count as new evidence.
        var continuing = Plans(Capture(), afterMotion, Memory, occasion);
        LastPressure = Pressure(continuing);
        var policy = F(Policy); var gate = OperationClaim.AtLeast(new Carrier(), F((Relation)LastPressure)[2], policy[0], new Carrier());
        var persistenceBefore = (long)Persistence.Value;
        var persistent = Quiet(gate) ? persistenceBefore + 1 : 0;
        var effort = Carrier.ReceiveTogether(G(new CarrierLocation(Persistence, persistent - persistenceBefore)));
        var review = Review(Capture(), afterMotion, out var rewritten);
        LastReview = review; LastRead = rewritten ? Observe(Capture()) : afterMotion;
        var step = TimeStepSequence.Step(new Carrier(), clock, (long)clock.Value, checked((long)clock.Value + 1), before,
            G(G(plans), negotiated, motion, effects, LastPressure, gate, effort, review), LastRead, F(LastRead)[3]);
        clock.Add(1); steps.Add(step); Timeline = TimeStepSequence.Create(new Carrier(), clock, initial, steps.ToArray());
        return new Transition(step, before, afterMotion, LastRead, G(plans), negotiated, motion, effects, review, rewritten);
    }

    private Relation[] Plans(IReadOnlyDictionary<Relation, Relation> samples, Relation read, Relation memory, IElement occasion) =>
        Active.Zip(Assessments(read), (o, assessment) => o.Propose(samples, assessment,
            F(memory).Cast<Relation>().FirstOrDefault(p => ReferenceEquals(p.Left, o.Surface))?.Right is { } previous && previous is not Placeholder ? previous : null,
            occasion)).ToArray();

    /// <summary>A selected receiver-side law: where a public port receives both
    /// signs, cap each request at its declared local span. Agreement passes
    /// unchanged. It asks no component which desired picture is correct.</summary>
    public Relation Negotiate(Relation[] requests)
    {
        var results = new List<IElement>(); var witnesses = new List<IElement>(); var passages = F(F(Policy)[6]).Cast<Relation>().ToArray();
        foreach (var group in requests.GroupBy(r => (Relation)F(r)[0]))
        {
            var passage = passages.FirstOrDefault(p => ReferenceEquals(F(p)[0], group.Key));
            if (passage is null) { results.AddRange(group); continue; }
            var axis = NegotiatingMachine.PortAxis(group.Key);
            var incompatible = group.Where(r => !ProjectionResidue.TryRead(F(r)[3], out var basis, out _, out _) ||
                !basis.SameIdentityAs(axis)).ToArray();
            if (incompatible.Length > 0)
            {
                // Clipping must not turn an equal-valued foreign basis into a
                // valid request. Retain the original group for receiver checks.
                results.AddRange(group);
                witnesses.Add(G(ReadWitness.Create(new Carrier(), G(group.Cast<IElement>().ToArray()), passage,
                    new Placeholder(), G(incompatible), G(axis)), G()));
                continue;
            }
            var pf = F(passage); var guardProofs = new List<IElement>(); var guardQuiet = true;
            foreach (var guard in F(pf[2]).Cast<Relation>())
            {
                TimeStepSequence.TryReadSpan((Relation)guard.Left, out var origin, out var end);
                TimeStepSequence.TryReadSpan((Relation)guard.Right, out var minimum, out var maximum);
                var selected = group.Where(r => (Fraction.Read(F(r)[3]) * new Fraction(end.Ticks - origin.Ticks)).N > 0).ToArray();
                var count = new CarrierLocation(minimum.Carrier, selected.Length);
                var claim = OperationClaim.Between(new Carrier(), count, minimum, maximum, new Carrier());
                var valid = OperationClaim.TryReadResolvedBetweenViolation(claim, out var violation) && violation.Ticks == 0;
                guardQuiet &= valid;
                guardProofs.Add(ReadWitness.Create(new Carrier(), G(selected), guard, G(count, claim), valid ? G() : G(claim), G(group.Cast<IElement>().ToArray())));
            }
            var guardRead = ReadWitness.Create(new Carrier(), G(group.Cast<IElement>().ToArray()), (Relation)pf[2], G(guardProofs.ToArray()),
                guardQuiet ? G() : G(guardProofs.ToArray()), passage);
            var bound = (Relation)pf[1]; IElement learned = G();
            if (guardQuiet && pf.Length == 4)
            {
                var binding = F(pf[3]); var program = new LocalResponseProgram((Relation)binding[0]);
                var context = RequestContext.Read(group.ToArray(), axis, program.Inputs);
                var prediction = program.Predict(context); var selection = program.Select(prediction);
                var projections = new List<IElement>(); IElement selectedBound = new Placeholder();
                if (F(selection)[2] is Relation action && TimeStepSequence.TryReadSpan(action, out var a, out var b))
                {
                    var first = Equation.PredictWitness(new Carrier(), (Relation)binding[1], a, axis);
                    var last = Equation.PredictWitness(new Carrier(), (Relation)binding[1], b, axis); projections.Add(first); projections.Add(last);
                    if (F(first)[2] is CarrierLocation from && F(last)[2] is CarrierLocation to)
                        selectedBound = bound = Relation.Segment(axis, from.Ticks, to.Ticks);
                }
                learned = ReadWitness.Create(new Carrier(), G(group.Cast<IElement>().ToArray()), (Relation)pf[3], selectedBound,
                    selectedBound is Placeholder ? G(selection) : G(), G(context, prediction, selection, G(projections.ToArray())));
            }
            TimeStepSequence.TryReadSpan(bound, out var low, out var high);
            foreach (var request in group)
            {
                var f = F(request); var raw = Fraction.Read(f[3]);
                var accepted = guardQuiet ? Fraction.Clamp(raw, new Fraction(low.Ticks), new Fraction(high.Ticks)) : raw;
                var bounded = ReadWitness.Create(new Carrier(), G(group.Cast<IElement>().ToArray()), passage, accepted.Encode(axis),
                    (raw - accepted).Encode(axis), pf.Length == 4 ? G(request, guardRead, learned) : G(request, guardRead));
                if (!guardQuiet) { results.Add(request); witnesses.Add(G(bounded, G())); continue; }
                var target = Equation.PredictWitness(new Carrier(), Equation.Sum(axis, Equation.UnitPort(axis), Equation.UnitPort(axis)),
                    new Relation(F(f[1])[2], accepted.Encode(axis)), axis);
                var signal = EvidenceObligation.Signal(group.Key, (Relation)f[1], target, G(request, bounded));
                results.Add(signal.Request); witnesses.Add(G(bounded, signal.Witness));
            }
        }
        return ReadWitness.Create(new Carrier(), G(requests), (Relation)F(Policy)[6], G(results.ToArray()), G(), G(witnesses.ToArray()));
    }

    private Relation Pressure(Relation[] plans)
    {
        var addressed = F(F(Policy)[5]); var positive = Fraction.Zero; var negative = Fraction.Zero; var inputs = new List<IElement>();
        foreach (var request in plans.SelectMany(EvidenceObligation.Requests).Where(r => addressed.Any(p => ReferenceEquals(p, F(r)[0]))))
        {
            ProjectionResidue.TryRead(F(request)[3], out var source, out _, out _);
            var calibration = Equation.Affine(source, 0, 1, ScoreAxis, 0, 1);
            var projected = Equation.PredictWitness(new Carrier(), calibration, F(request)[3], ScoreAxis);
            var value = Fraction.Read(F(projected)[2]); if (value.N > 0) positive += value; else negative -= value;
            inputs.Add(G(request, projected));
        }
        var amount = positive < negative ? positive : negative;
        var whole = amount.Truncate(1);
        var selectedSide = OperationClaim.AtMost(new Carrier(), amount.Encode(ScoreAxis),
            (positive < negative ? negative : positive).Encode(ScoreAxis), new Carrier());
        return ReadWitness.Create(new Carrier(), G(inputs.ToArray()), G(F(Policy)[5], ScoreAxis), new CarrierLocation(ScoreAxis, whole), (amount - new Fraction(whole)).Encode(ScoreAxis),
            G(positive.Encode(ScoreAxis), negative.Encode(ScoreAxis), selectedSide));
    }
    private IElement Review(Dictionary<Relation, Relation> samples, Relation observed, out bool rewritten)
    {
        rewritten = false; var p = F(Policy);
        var demand = OperationClaim.AtLeast(new Carrier(), new CarrierLocation(Persistence, (long)Persistence.Value), p[1], new Carrier());
        TimeStepSequence.TryReadSpan((Relation)p[2], out _, out var limit);
        var budget = OperationClaim.AtMost(new Carrier(), new CarrierLocation(Reviews, (long)Reviews.Value + 1), limit, new Carrier());
        if (Selected is not Placeholder || !Quiet(demand) || !Quiet(budget) || Unknown(observed) != 0)
            return ReadWitness.Create(new Carrier(), observed, G(demand, budget), new Placeholder(), G(demand, budget), G(Selected));
        Reviews.Add(1);
        var baseline = Probe(null, samples); var candidates = F(Proposals).Cast<Relation>().Select(p => Qualify(baseline, Probe(p, samples))).ToArray();
        var eligible = candidates.Where(Eligible).ToArray();
        if (eligible.Length == 0) return ReadWitness.Create(new Carrier(), observed, G(demand, budget), G(new Placeholder(), baseline, G(candidates)), G(candidates), G(Policy));
        var best = eligible.Aggregate((a, b) => Fraction.Read(F(F(a)[2])[1]) < Fraction.Read(F(F(b)[2])[1]) ? a : b);
        var proposal = (Relation)F(best)[1]; var f = F(proposal);
        if (!Joint.TryRewrite((Relation)f[0], (Relation)f[1], (Relation)f[2], out var replacement))
            return ReadWitness.Create(new Carrier(), observed, G(demand, budget), new Placeholder(), G(replacement), G(baseline, G(candidates)));
        var routes = CoupledRecipes.BindRules(Joint, (Relation)f[3]); Selected = proposal; rewritten = true;
        return ReadWitness.Create(new Carrier(), observed, G(demand, budget), G(proposal, baseline, G(candidates)), G(), G(replacement, routes, best));
    }

    // A local roof fork plus frozen external PUBLIC samples. Other machines'
    // private definitions and actuators are deliberately absent from the fork.
    // Result = G(initial demand, after demand, unresolved paths, unknown count).
    public Relation Probe(Relation? proposal, Dictionary<Relation, Relation>? samples = null)
    {
        samples ??= Capture();
        var focus = (Relation)Program.CaptureObservation(G(Joint.Definition, G(Joint.Mechanical.Ports.ToArray()),
            G(Joint.Mechanical.Responses.ToArray()), Joint.RequestedPaths, proposal ?? (IElement)new Placeholder()));
        var f = F(focus); var local = new JointAssembly((Relation)f[0], (Relation)f[3]);
        CopyRoutes(local.Mechanical, F(f[1]).Cast<Relation>().ToArray(), F(f[2]).Cast<Relation>().ToArray());
        IElement replacement = new Placeholder();
        if (f[4] is Relation candidate)
        {
            var parts = F(candidate);
            if (!local.TryRewrite((Relation)parts[0], (Relation)parts[1], (Relation)parts[2], out var attempt))
                return ReadWitness.Create(new Carrier(), G(focus, G(samples.Values.Cast<IElement>().ToArray())), proposal!, new Placeholder(), G(attempt), G());
            replacement = G(attempt, CoupledRecipes.BindRules(local, (Relation)parts[3]));
        }
        var history = new List<IElement>(); var memory = Memory; var previewClock = new Carrier();
        var before = Observe(samples); var env = new Dictionary<Relation, Relation>(samples);
        TimeStepSequence.TryReadSpan((Relation)F(Policy)[3], out var from, out var to);
        for (var pass = from.Ticks; pass < to.Ticks; pass++)
        {
            ProjectPublications(local.Mechanical, env, focus);
            var read = Observe(env); var plans = Plans(env, read, memory, G(focus, new CarrierLocation(previewClock, pass)));
            var adapted = new List<Relation>();
            var negotiated = Negotiate(plans.SelectMany(EvidenceObligation.Requests).ToArray()); history.Add(negotiated);
            foreach (var request in F(F(negotiated)[2]).Cast<Relation>())
            {
                var port = (Relation)F(request)[0]; var index = Array.FindIndex(Joint.Mechanical.Ports.ToArray(), p => ReferenceEquals(p, port));
                if (index < 0) continue;
                var ownPort = local.Mechanical.Ports[index]; var peer = (Relation)F(request)[2]; var peerPort = (Relation)F(peer)[0];
                var link = new NegotiationLink(ownPort, peerPort);
                adapted.Add(NegotiationLink.Requests(link.Plan(local.Mechanical.Publish(ownPort), peer))[0]);
            }
            if (adapted.Count > 0)
            {
                if (!local.Mechanical.Apply(adapted, out var receipt)) throw new InvalidOperationException("A private preview request was stale.");
                history.Add(receipt); local.ObserveTransition(receipt);
            }
            memory = G(Active.Zip(Assessments(read), (o, assessment) => (IElement)new Relation(o.Surface,
                EvidenceObligation.Resolved(assessment) ? EvidenceObligation.Value(assessment) : new Placeholder())).ToArray());
        }
        ProjectPublications(local.Mechanical, env, focus); var after = Observe(env);
        var result = G(Cost(before), Cost(after), new CarrierLocation(ScoreAxis, CoupledSettlement.OpenPaths(local)),
            new CarrierLocation(ScoreAxis, Unknown(after)));
        return ReadWitness.Create(new Carrier(), G(Joint.Definition, G(samples.Values.Cast<IElement>().ToArray())), proposal ?? (IElement)new Placeholder(),
            result, G(F(after)[3]), G(focus, replacement, G(history.ToArray()), local.Surface, before, after));
    }
    public static bool ProbeResolved(Relation probe) => F(probe)[2] is Relation r && r.TryReadOrderedGroup(out var f) && f.Length == 4 && Fraction.Read(f[3]) == Fraction.Zero;
    private Relation Qualify(Relation baseline, Relation candidate)
    {
        if (!ProbeResolved(baseline) || !ProbeResolved(candidate))
            return ReadWitness.Create(new Carrier(), baseline, F(candidate)[1], F(candidate)[2], G(candidate), G(candidate));
        var result = F(F(candidate)[2]); var original = F(F(baseline)[2]);
        var advantage = Equation.PredictWitness(new Carrier(), Equation.Difference(ScoreAxis, ScoreAxis, ScoreAxis),
            new Relation(result[1], original[1]), ScoreAxis);
        var benefit = OperationClaim.AtLeast(new Carrier(), F(advantage)[2], F(Policy)[4], new Carrier());
        var paths = OperationClaim.AtMost(new Carrier(), result[2], new CarrierLocation(ScoreAxis, CoupledSettlement.OpenPaths(Joint)), new Carrier());
        var claims = G(benefit, paths);
        return ReadWitness.Create(new Carrier(), baseline, F(candidate)[1], F(candidate)[2],
            Quiet(benefit) && Quiet(paths) ? G() : claims, G(candidate, advantage, claims));
    }
    public static bool Eligible(Relation review) => ProbeResolved(review) && F(review)[4] is Relation proof &&
        proof.TryReadOrderedGroup(out var p) && p.Length == 3 && F(p[2]) is { Length: 2 } claims && claims.All(c => Quiet((Relation)c));
    private void ProjectPublications(NegotiatingMachine local, Dictionary<Relation, Relation> samples, IElement focus)
    {
        foreach (var pair in local.Ports.Zip(Joint.Mechanical.Ports))
        {
            var publication = local.Publish(pair.First); var source = NegotiatingMachine.PortAxis(pair.First); var target = NegotiatingMachine.PortAxis(pair.Second);
            var map = Equation.Affine(source, 0, 1, target, 0, 1);
            var projection = Equation.PredictWitness(new Carrier(), map, F(publication)[2], target);
            // This is explicitly a hypothetical evaluation binding, not a new
            // publication event on the live participant's clock.
            samples[pair.Second] = G(pair.Second, G(focus, publication), F(projection)[2], G(projection, new Relation(pair.First, pair.Second)));
        }
    }
    private static void CopyRoutes(NegotiatingMachine target, Relation[] previousPorts, Relation[] routes)
    {
        foreach (var route in routes)
        {
            var f = F(route); var index = Array.FindIndex(previousPorts, p => ReferenceEquals(p, f[0])); var map = (Relation)f[1];
            TimeStepSequence.TryReadSpan((Relation)map.Left, out var a, out var b); TimeStepSequence.TryReadSpan((Relation)map.Right, out var c, out var d);
            ProjectionResidue.TryRead(f[2], out _, out var n, out var denominator); var cap = F(f[3]);
            TimeStepSequence.TryReadSpan((Relation)cap[0], out var low, out var high); TimeStepSequence.TryReadSpan((Relation)cap[1], out _, out var step);
            TimeStepSequence.TryReadSpan((Relation)cap[2], out _, out var quantum);
            target.AddResponse(target.Ports[index], c.Carrier, d.Ticks - c.Ticks, b.Ticks - a.Ticks,
                (long)n, (long)denominator, low.Ticks, high.Ticks, step.Ticks, quantum.Ticks);
        }
    }
    private static bool Quiet(Relation claim) => OperationClaim.TryReadResolvedViolation(claim, out var value) && value.Ticks == 0;
}
