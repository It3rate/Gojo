using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>A supplied, bounded experiment policy. Connected offer evidence and
/// finite laws select a trial; an exact quiet grant admits dispatch. Neither
/// predicted usefulness nor a grant proves that the wider world is isolated.</summary>
public sealed class LocalTrialMachine
{
    // contract = Closed(identity, sensor, owner, comparison basis/domain,
    //   extent port, quiet port, allowance port/clamp, truth basis/domain,
    //   eligibility law, ordered directions, response selector, clock, journal)
    // offer = direction -> Closed(feasible support, distinguishing support, evidence)
    // need = Closed(wanted support, goal/evidence)
    // state = Closed(Closed(request -> (packet -> selection)), Closed(grant -> dispatch))
    // request = (client identity -> quiet port) -> occurrence
    // dispatch = (request -> packet) -> captured allowance transition
    // archive = contract -> (state -> (request history -> timeline))
    private readonly Relation contract;
    private readonly IElement[] fields;
    private readonly IElement[] signs;
    private readonly IElement[] truth;
    private readonly Carrier allowance;
    private readonly Carrier clock;
    private readonly Carrier journal;
    private readonly FiniteLawPlan eligibility;
    private readonly ExtentResponseArbiter selector;
    private Relation state;
    private Relation requests;
    private Relation timeline;

    public Relation Surface { get; private set; }
    public Relation QuietPort => (Relation)fields[6];
    public Relation AllowancePort => (Relation)fields[7];
    public Relation ExtentPort => (Relation)fields[5];
    public long Allowance => checked((long)allowance.Value);
    public bool Outstanding => Snapshot().Outstanding;
    public bool NeedsGrant => Snapshot() is { Outstanding: true, Dispatched: false };

    public sealed record SnapshotRead(bool Outstanding, bool Dispatched, long Allowance,
        long Beat, Relation? Request, Relation? Packet, Relation? Grant, Relation Surface);
    public sealed record SelectionRead(bool Selected, Relation? Request, Relation? Packet,
        long? Delta, long Allowance, Relation Witness);
    public sealed record OperationRead(bool Accepted, Relation? Packet, Relation? Request,
        Relation? Grant, Relation Witness);

    private LocalTrialMachine(Relation archive)
    {
        if (archive is not { Left: Relation configuration, Right: Relation current } ||
            current is not { Left: Relation held, Right: Relation memory } ||
            memory is not { Left: Relation occurrences, Right: Relation history })
            throw new ArgumentException("Expected a local trial contract, held request and history.", nameof(archive));
        fields = TrialGrammar.Entries(configuration);
        if (fields is not [Placeholder, Placeholder, Placeholder, Carrier basis, Relation domain,
            Relation extent, Relation quiet, Relation budgetPort, Relation clamp, Carrier truthBasis,
            Relation truthDomain, Relation law, Relation order, Relation chooser, Relation timePort, Carrier holder] ||
            !TrialGrammar.Port(extent, out _) || quiet is not { Left: Placeholder, Right: Placeholder } ||
            !TrialGrammar.Port(budgetPort, out var budget) || !budget.IsAttached(clamp) ||
            !TrialGrammar.Clamp(clamp, budget, out _) || !TrialGrammar.Port(timePort, out var time) ||
            !FiniteLaw.TryRead(law, out var left, out var right, out var output, out _) ||
            !ReferenceEquals(left, truthDomain) || !ReferenceEquals(right, truthDomain) || !ReferenceEquals(output, truthDomain))
            throw new ArgumentException("Expected the exact bounded trial surface.", nameof(archive));
        signs = TrialGrammar.Entries(domain); truth = TrialGrammar.Entries(truthDomain);
        var directions = TrialGrammar.Entries(order);
        if (!TrialGrammar.Coordinates(signs, basis, [-1, 0, 1]) ||
            !TrialGrammar.Coordinates(truth, truthBasis, [0, 1]) || directions.Length != 2 ||
            !directions.Any(d => TrialGrammar.Same(d, signs[0])) || !directions.Any(d => TrialGrammar.Same(d, signs[2])))
            throw new ArgumentException("Expected two ordered opposed probes and a Boolean evidence basis.", nameof(archive));
        selector = ExtentResponseArbiter.FromSurface(chooser);
        var selectorFields = TrialGrammar.Entries(chooser);
        if (!ReferenceEquals(selectorFields[1], fields[1]) || !ReferenceEquals(selectorFields[2], fields[2]) ||
            !ReferenceEquals(selectorFields[3], fields[3]) || !ReferenceEquals(selectorFields[4], fields[4]))
            throw new ArgumentException("The trial selector must address the same receiver.", nameof(archive));
        eligibility = FiniteLawPlan.Capture(law) ?? throw new ArgumentException("Incomplete trial eligibility law.");
        contract = configuration; allowance = budget; clock = time; journal = holder;
        state = held; requests = occurrences; timeline = history; Surface = archive;
        ValidateState();
    }

    public static LocalTrialMachine Create(CoupledExtentMachine receiver, Relation quietPort,
        long allowance = 2, bool preferPositive = true)
    {
        if (allowance < 0) throw new ArgumentOutOfRangeException(nameof(allowance));
        var budget = new Carrier(); var clamp = TrialGrammar.MakeClamp(budget, allowance);
        budget.Attach(clamp); budget.Add((ulong)allowance);
        var truth = new Carrier(); var values = new IElement[] { new CarrierLocation(truth, 0), new CarrierLocation(truth, 1) };
        var domain = TrialGrammar.S(values); var rows = new List<Relation>();
        for (var a = 0; a < 2; a++) for (var b = 0; b < 2; b++)
            rows.Add(FiniteLaw.Row(values[a], values[b], TrialGrammar.S(values[a & b])));
        var law = FiniteLaw.Create(domain, domain, domain, rows.ToArray());
        var directions = TrialGrammar.Entries(receiver.ComparisonDomain);
        var time = new Carrier();
        var configuration = TrialGrammar.S(new Placeholder(), receiver.Sensor, receiver.Owner,
            receiver.ComparisonBasis, receiver.ComparisonDomain, receiver.ExtentPort, quietPort,
            new Relation(budget.ValueSlot, budget), clamp, truth, domain, law,
            preferPositive ? TrialGrammar.S(directions[2], directions[0]) : TrialGrammar.S(directions[0], directions[2]),
            ExtentResponseArbiter.Create(receiver, "first").Surface, new Relation(time.ValueSlot, time), new Carrier());
        return new(new Relation(configuration,
            new Relation(TrialGrammar.S(TrialGrammar.S(), TrialGrammar.S()), new Relation(TrialGrammar.S(), TrialGrammar.S()))));
    }

    /// <summary>These are supplied local observations, not hidden capacity or
    /// causal reads. Unknown support stays open and cannot authorize a probe.</summary>
    public Relation Offer(int sign, bool? feasible, bool? distinguishes, IElement evidence)
    {
        if (sign is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(sign));
        return new(signs[sign + 1], TrialGrammar.S(Support(feasible), Support(distinguishes), evidence));
    }

    public Relation Need(bool? wanted, IElement evidence) => TrialGrammar.S(Support(wanted), evidence);

    /// <summary>Selection does not spend allowance. While a request is held,
    /// later calls return that same request rather than sampling a replacement.</summary>
    public SelectionRead Select(Relation positiveOffer, Relation negativeOffer, Relation need, bool compiled = true)
    {
        var before = Snapshot();
        if (before.Outstanding)
            return new(true, before.Request, before.Packet, PacketDelta(before.Packet!), before.Allowance,
                new Relation(Surface, before.Surface));
        var reason = TrialGrammar.Closed(need, out var needFields) && needFields is [Relation support, _]
            ? support : StructuralConstraint.Open(need);
        var plus = Candidate(positiveOffer, signs[2], reason, compiled);
        var minus = Candidate(negativeOffer, signs[0], reason, compiled);
        var order = TrialGrammar.Entries((Relation)fields[12]);
        var positiveFirst = TrialGrammar.Same(order[0], signs[2]);
        var choice = selector.Evaluate(positiveFirst ? plus.Packet : minus.Packet,
            positiveFirst ? minus.Packet : plus.Packet, compiled);
        var proof = TrialGrammar.S(positiveOffer, negativeOffer, need, plus.Witness, minus.Witness, choice.Witness,
            new Relation(AllowancePort, new CarrierLocation(allowance, Allowance)));
        Relation? request = null;
        var selected = choice.Resolved && choice.Mask is 1 or 4 && Allowance > 0 && allowance.Tension == 0;
        if (selected)
        {
            request = new Relation(new Relation(fields[0], QuietPort), new Placeholder());
            state = TrialGrammar.S(TrialGrammar.S(new Relation(request, new Relation(choice.Packet, proof))), TrialGrammar.S());
            requests = TrialGrammar.S([.. TrialGrammar.Entries(requests), request]);
        }
        var transition = Finish(before.Surface, proof, selected);
        return new(selected, request, selected ? choice.Packet : null, selected ? PacketDelta(choice.Packet) : null,
            Allowance, transition);
    }

    /// <summary>The exact active grant from the addressed quiet service is
    /// necessary. One dispatch spends one local allowance, including a later
    /// blocked actuator attempt; predicted success is never substituted.</summary>
    public OperationRead Dispatch(TrialQuietPort quiet, Relation grant)
    {
        var before = Snapshot();
        var accepted = before.Outstanding && !before.Dispatched && Allowance > 0 && allowance.Tension == 0 &&
            ReferenceEquals(quiet.PublicPort, QuietPort) && quiet.IsActiveGrant(grant, before.Request!);
        if (accepted)
        {
            var old = new CarrierLocation(allowance, Allowance); allowance.Remove(1);
            var spend = new Relation(old, new CarrierLocation(allowance, Allowance));
            var dispatch = new Relation(new Relation(before.Request!, before.Packet!), new Relation(AllowancePort, spend));
            state = TrialGrammar.S(TrialGrammar.Entries(state)[0], TrialGrammar.S(new Relation(grant, dispatch)));
        }
        var witness = Finish(before.Surface, grant, accepted);
        return new(accepted, accepted ? before.Packet : null, before.Request, accepted ? grant : null, witness);
    }

    /// <summary>A trial completes only against the actual latest consumed packet
    /// of this receiver, while its quiet grant is still active. This establishes
    /// only the declared routed scope. External or unadvertised causes remain
    /// outside the claim. Completion never releases the other owner's gate.</summary>
    public OperationRead Complete(CoupledExtentMachine receiver, CoupledExtentMachine.StepRead response, TrialQuietPort quiet)
    {
        var before = Snapshot();
        var accepted = before.Dispatched && ReferenceEquals(receiver.ExtentPort, ExtentPort) &&
            ReferenceEquals(quiet.PublicPort, QuietPort) && quiet.IsActiveGrant(before.Grant!, before.Request!) &&
            response.ConsumedEvent && ReferenceEquals(receiver.History.LastOrDefault(), response.Surface) &&
            TimeStepSequence.TryReadStep(response.Surface, out _, out var start, out var end, out _, out var transition, out _, out _) &&
            end.Ticks - start.Ticks == 1 &&
            transition is Relation { Left: Relation { Left: Relation actual } } && ReferenceEquals(actual, before.Packet) &&
            receiver.Snapshot().Value == response.After.Value && receiver.Snapshot().Tension == response.After.Tension;
        if (accepted) state = TrialGrammar.S(TrialGrammar.S(), TrialGrammar.S());
        var witness = Finish(before.Surface, response.Surface, accepted);
        return new(accepted, accepted ? before.Packet : null, before.Request, before.Grant, witness);
    }

    public OperationRead Cancel(Relation request)
    {
        var before = Snapshot();
        var accepted = before.Outstanding && !before.Dispatched && ReferenceEquals(before.Request, request);
        if (accepted) state = TrialGrammar.S(TrialGrammar.S(), TrialGrammar.S());
        return new(accepted, null, request, null, Finish(before.Surface, request, accepted));
    }

    public SnapshotRead Snapshot()
    {
        ValidateState();
        var rows = TrialGrammar.Entries(state);
        var held = TrialGrammar.Entries((Relation)rows[0]).FirstOrDefault() as Relation;
        var dispatched = TrialGrammar.Entries((Relation)rows[1]).FirstOrDefault() as Relation;
        var packet = held?.Right is Relation selection ? (Relation)selection.Left : null;
        return new(held is not null, dispatched is not null, Allowance, checked((long)clock.Value),
            held?.Left as Relation, packet, dispatched?.Left as Relation,
            TrialGrammar.S(state, new CarrierLocation(allowance, Allowance), new CarrierLocation(clock, checked((long)clock.Value))));
    }

    public Relation ArchiveSurface() => Surface;
    public byte[] Encode() => GojoNumericFormat.Encode(Surface);
    public static LocalTrialMachine Restore(Relation archive) => new(archive);
    public static LocalTrialMachine Decode(ReadOnlySpan<byte> bytes) => GojoNumericFormat.Decode(bytes) is [Relation archive]
        ? Restore(archive) : throw new ArgumentException("Expected one local-trial root.", nameof(bytes));

    private (Relation Packet, Relation Witness) Candidate(Relation offer, IElement expected, Relation need, bool compiled)
    {
        var valid = TrialGrammar.Same(offer.Left, expected) && offer.Right is Relation body &&
            TrialGrammar.Closed(body, out var rows) && rows is [Relation, Relation, _];
        IElement[] inputs = valid ? TrialGrammar.Entries((Relation)offer.Right) : [StructuralConstraint.Open(), StructuralConstraint.Open(), offer];
        var read = compiled ? eligibility.Evaluate((Relation)inputs[0], (Relation)inputs[1]) :
            FiniteLaw.Evaluate((Relation)fields[11], (Relation)inputs[0], (Relation)inputs[1]);
        var first = TruthResult(read);
        var gated = compiled ? eligibility.Evaluate(first, need) : FiniteLaw.Evaluate((Relation)fields[11], first, need);
        var admitted = TruthResult(gated);
        var good = valid && TrialGrammar.Closed(admitted, out var values) && values.Length == 1 && TrialGrammar.Same(values[0], truth[1]);
        var packet = QualitativePublication.Create(fields[1], fields[2], new Placeholder(), fields[3], (Relation)fields[4],
            good ? TrialGrammar.S(expected) : StructuralConstraint.Open(), fields[0]);
        return (packet, new Relation(offer, TrialGrammar.S(read, gated, packet)));
    }

    private Relation TruthResult(Relation read) => FiniteLaw.TryReadResult(read, out var answer, out var residue, out _) &&
        TrialGrammar.Closed(residue, out var pending) && pending.Length == 0 ? answer : StructuralConstraint.Open(read);
    private Relation Support(bool? value) => value is null ? StructuralConstraint.Open() : TrialGrammar.S(truth[value.Value ? 1 : 0]);
    private long? PacketDelta(Relation packet) => QualitativePublication.TryRead(packet, out _, out _, out _, out _, out _, out var support, out _) &&
        TrialGrammar.Closed(support, out var items) && items is [CarrierLocation at] ? at.Ticks : null;

    private Relation Finish(IElement before, IElement cause, bool accepted)
    {
        var start = checked((long)clock.Value); clock.Add(1);
        var after = Snapshot().Surface;
        var witness = TimeStepSequence.Step(journal, clock, start, checked((long)clock.Value), before,
            new Relation(contract, new Relation(cause, accepted ? TrialGrammar.S(cause) : TrialGrammar.S())),
            after, accepted ? TrialGrammar.S() : TrialGrammar.S(cause));
        timeline = TrialGrammar.S([.. TrialGrammar.Entries(timeline), witness]);
        Surface = new Relation(contract, new Relation(state, new Relation(requests, timeline)));
        return witness;
    }

    private void ValidateState()
    {
        if (!TrialGrammar.Closed(state, out var states) || states is not [Relation held, Relation dispatches] ||
            !TrialGrammar.Closed(held, out var pending) || !TrialGrammar.Closed(dispatches, out var dispatched) ||
            pending.Length > 1 || dispatched.Length > pending.Length || allowance.Tension != 0 ||
            !TrialGrammar.Closed(requests, out var past) || !TrialGrammar.Closed(timeline, out var steps) ||
            past.Any(r => r is not Relation priorRequest || !ValidRequest(priorRequest)) ||
            past.Distinct(ReferenceEqualityComparer.Instance).Count() != past.Length || steps.Any(s => s is not Relation))
            throw new ArgumentException("Malformed local trial state.");
        if (pending.Length == 0) return;
        if (pending[0] is not Relation { Left: Relation request, Right: Relation choice } || !ValidRequest(request) ||
            !past.Any(item => ReferenceEquals(item, request)) || choice.Left is not Relation packet || PacketDelta(packet) is not (-1 or 1) ||
            !QualitativePublication.TryRead(packet, out var sensor, out var owner, out _, out var basis, out var domain, out _, out _) ||
            !ReferenceEquals(sensor, fields[1]) || !ReferenceEquals(owner, fields[2]) ||
            !ReferenceEquals(basis, fields[3]) || !ReferenceEquals(domain, fields[4]))
            throw new ArgumentException("Pending request does not retain its selected packet.");
        if (dispatched.Length == 0) return;
        if (dispatched[0] is not Relation { Left: Relation grant, Right: Relation sent } ||
            grant is not { Left: Relation granted, Right: Placeholder } || !ReferenceEquals(granted.Left, QuietPort) ||
            !ReferenceEquals(granted.Right, request) || sent.Left is not Relation delivered ||
            !ReferenceEquals(delivered.Left, request) || !ReferenceEquals(delivered.Right, packet))
            throw new ArgumentException("The dispatched trial must retain the exact request, packet and quiet grant.");
    }

    private bool ValidRequest(Relation request) => request is { Left: Relation target, Right: Placeholder } &&
        ReferenceEquals(target.Left, fields[0]) && ReferenceEquals(target.Right, QuietPort);
}

/// <summary>A cooperative quiet interval for one advertised response route.
/// While occupied, its native finite law maps routed commands to hold. It does
/// not freeze a carrier or claim that private/unrouted causes cannot change it.</summary>
public sealed class TrialQuietPort
{
    // contract = Closed(public port, receiver sensor/owner/basis/domain,
    //   occupancy port/clamp, quiet basis/domain, suppression law, clock, journal)
    // state = Closed(request -> grant)
    // grant = (public port -> exact request) -> opaque identity
    // archive = contract -> (state -> (previous requests -> timeline))
    private readonly Relation contract;
    private readonly IElement[] fields;
    private readonly IElement[] signs;
    private readonly IElement[] modes;
    private readonly Carrier occupancy;
    private readonly Carrier clock;
    private readonly Carrier journal;
    private readonly FiniteLawPlan plan;
    private Relation state;
    private Relation requests;
    private Relation timeline;

    public Relation Surface { get; private set; }
    public Relation PublicPort => (Relation)fields[0];
    public bool Quiet => occupancy.Value == 1;
    public sealed record GrantRead(bool Accepted, Relation? Grant, Relation Witness);
    public sealed record FilterRead(Relation Packet, byte Mask, bool Quiet, Relation Witness);
    public sealed record SnapshotRead(bool Quiet, Relation? Request, Relation? Grant, Relation Surface);

    private TrialQuietPort(Relation archive)
    {
        if (archive is not { Left: Relation configuration, Right: Relation current } ||
            current is not { Left: Relation held, Right: Relation memory } ||
            memory is not { Left: Relation events, Right: Relation history }) throw new ArgumentException("Expected quiet service archive.");
        fields = TrialGrammar.Entries(configuration);
        if (fields is not [Relation port, Placeholder, Placeholder owner, Carrier basis, Relation domain,
            Relation slotPort, Relation clamp, Carrier quietBasis, Relation quietDomain, Relation law,
            Relation timePort, Carrier holder] || port is not { Left: Placeholder } || !ReferenceEquals(port.Right, owner) ||
            !TrialGrammar.Port(slotPort, out var slot) || !slot.IsAttached(clamp) ||
            !TrialGrammar.Clamp(clamp, slot, out var capacity) || capacity != 1 || !TrialGrammar.Port(timePort, out var time) ||
            !FiniteLaw.TryRead(law, out var left, out var right, out var output, out _) ||
            !ReferenceEquals(left, domain) || !ReferenceEquals(right, quietDomain) || !ReferenceEquals(output, domain))
            throw new ArgumentException("Expected exact quiet response passage.");
        signs = TrialGrammar.Entries(domain); modes = TrialGrammar.Entries(quietDomain);
        if (!TrialGrammar.Coordinates(signs, basis, [-1, 0, 1]) || !TrialGrammar.Coordinates(modes, quietBasis, [0, 1]))
            throw new ArgumentException("Expected comparison and quiet domains.");
        plan = FiniteLawPlan.Capture(law) ?? throw new ArgumentException("Incomplete quiet response law.");
        contract = configuration; occupancy = slot; clock = time; journal = holder;
        state = held; requests = events; timeline = history; Surface = archive; ValidateState();
    }

    public static TrialQuietPort Create(CoupledExtentMachine receiver)
    {
        var slot = new Carrier(); var clamp = TrialGrammar.MakeClamp(slot, 1); slot.Attach(clamp);
        var quiet = new Carrier(); var modes = new IElement[] { new CarrierLocation(quiet, 0), new CarrierLocation(quiet, 1) };
        var domain = TrialGrammar.S(modes); var signs = TrialGrammar.Entries(receiver.ComparisonDomain); var rows = new List<Relation>();
        for (var a = 0; a < 3; a++) for (var b = 0; b < 2; b++)
            rows.Add(FiniteLaw.Row(signs[a], modes[b], TrialGrammar.S(signs[b == 1 ? 1 : a])));
        var law = FiniteLaw.Create(receiver.ComparisonDomain, domain, receiver.ComparisonDomain, rows.ToArray());
        var time = new Carrier(); var port = new Relation(new Placeholder(), receiver.Owner);
        var configuration = TrialGrammar.S(port, receiver.Sensor, receiver.Owner, receiver.ComparisonBasis,
            receiver.ComparisonDomain, new Relation(slot.ValueSlot, slot), clamp, quiet, domain, law,
            new Relation(time.ValueSlot, time), new Carrier());
        return new(new Relation(configuration, new Relation(TrialGrammar.S(), new Relation(TrialGrammar.S(), TrialGrammar.S()))));
    }

    /// <summary>The one-slot service grants the first available addressed
    /// request. Repeating an active request returns its same grant; a released
    /// occurrence cannot acquire another interval.</summary>
    public GrantRead Grant(Relation request)
    {
        var before = Snapshot();
        if (Quiet && ReferenceEquals(before.Request, request))
            return new(true, before.Grant, new Relation(Surface, before.Surface));
        var accepted = !Quiet && request is { Left: Relation addressed, Right: Placeholder } &&
            addressed.Left is Placeholder && ReferenceEquals(addressed.Right, PublicPort) &&
            !TrialGrammar.Entries(requests).Any(previous => ReferenceEquals(previous, request));
        Relation? grant = null;
        if (accepted)
        {
            grant = new Relation(new Relation(PublicPort, request), new Placeholder());
            state = TrialGrammar.S(new Relation(request, grant));
            requests = TrialGrammar.S([.. TrialGrammar.Entries(requests), request]); occupancy.Add(1);
        }
        return new(accepted, grant, Finish(before.Surface, request, accepted));
    }

    public bool IsActiveGrant(Relation grant, Relation request) => Snapshot() is var active && active.Quiet &&
        ReferenceEquals(active.Request, request) && ReferenceEquals(active.Grant, grant);

    public GrantRead Release(Relation grant)
    {
        var before = Snapshot(); var accepted = before.Quiet && ReferenceEquals(before.Grant, grant);
        if (accepted) { state = TrialGrammar.S(); occupancy.Remove(1); }
        return new(accepted, accepted ? grant : null, Finish(before.Surface, grant, accepted));
    }

    public FilterRead Filter(Relation packet, bool compiled = true)
    {
        var valid = QualitativePublication.TryRead(packet, out var sensor, out var owner, out var occurrence,
            out var basis, out var domain, out var support, out _) && ReferenceEquals(sensor, fields[1]) &&
            ReferenceEquals(owner, fields[2]) && ReferenceEquals(basis, fields[3]) && ReferenceEquals(domain, fields[4]);
        var input = valid ? support : StructuralConstraint.Open(packet);
        var mode = TrialGrammar.S(modes[Quiet ? 1 : 0]);
        var read = compiled ? plan.Evaluate(input, mode) : FiniteLaw.Evaluate((Relation)fields[9], input, mode);
        var complete = FiniteLaw.TryReadResult(read, out var answer, out var remainder, out _) &&
            TrialGrammar.Closed(remainder, out var pending) && pending.Length == 0;
        if (!valid || !complete) answer = StructuralConstraint.Open(read);
        var output = QualitativePublication.Create(fields[1], fields[2], valid ? occurrence : new Placeholder(),
            fields[3], (Relation)fields[4], answer, PublicPort.Left);
        byte mask = 0;
        if (TrialGrammar.Closed(answer, out var values)) foreach (var value in values)
        {
            var index = Array.FindIndex(signs, s => TrialGrammar.Same(s, value));
            if (index >= 0) mask |= (byte)(1 << index);
        }
        return new(output, mask, Quiet, new Relation(new Relation(contract, packet), TrialGrammar.S(Snapshot().Surface, read, output)));
    }

    public SnapshotRead Snapshot()
    {
        ValidateState(); var held = TrialGrammar.Entries(state).FirstOrDefault() as Relation;
        return new(Quiet, held?.Left as Relation, held?.Right as Relation,
            TrialGrammar.S(state, new CarrierLocation(occupancy, checked((long)occupancy.Value)),
                new CarrierLocation(clock, checked((long)clock.Value))));
    }
    public Relation ArchiveSurface() => Surface;
    public byte[] Encode() => GojoNumericFormat.Encode(Surface);
    public static TrialQuietPort Restore(Relation archive) => new(archive);
    public static TrialQuietPort Decode(ReadOnlySpan<byte> bytes) => GojoNumericFormat.Decode(bytes) is [Relation root]
        ? Restore(root) : throw new ArgumentException("Expected one quiet-service root.", nameof(bytes));

    private Relation Finish(IElement before, IElement cause, bool accepted)
    {
        var start = checked((long)clock.Value); clock.Add(1);
        var witness = TimeStepSequence.Step(journal, clock, start, checked((long)clock.Value), before,
            new Relation(contract, new Relation(cause, accepted ? TrialGrammar.S(cause) : TrialGrammar.S())),
            Snapshot().Surface, accepted ? TrialGrammar.S() : TrialGrammar.S(cause));
        timeline = TrialGrammar.S([.. TrialGrammar.Entries(timeline), witness]);
        Surface = new Relation(contract, new Relation(state, new Relation(requests, timeline)));
        return witness;
    }

    private void ValidateState()
    {
        if (!TrialGrammar.Closed(state, out var held) || held.Length > 1 ||
            occupancy.Tension != 0 || occupancy.Value != (ulong)held.Length ||
            !TrialGrammar.Closed(requests, out var prior) || prior.Any(p => p is not Relation) ||
            prior.Distinct(ReferenceEqualityComparer.Instance).Count() != prior.Length ||
            !TrialGrammar.Closed(timeline, out var history) || history.Any(h => h is not Relation))
            throw new ArgumentException("Quiet occupancy and held interval disagree.");
        if (held.Length == 0) return;
        if (held[0] is not Relation { Left: Relation request, Right: Relation grant } ||
            !prior.Any(p => ReferenceEquals(p, request)) || request is not { Left: Relation addressed, Right: Placeholder } ||
            !ReferenceEquals(addressed.Right, PublicPort) || grant is not { Left: Relation source, Right: Placeholder } ||
            !ReferenceEquals(source.Left, PublicPort) || !ReferenceEquals(source.Right, request))
            throw new ArgumentException("Quiet grant must retain its exact addressed request.");
    }
}

internal static class TrialGrammar
{
    internal static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    internal static bool Closed(Relation scope, out IElement[] items) =>
        StructuralConstraint.TryReadScope(scope, out items, out var seam, out var tail) && seam.Equals(tail);
    internal static IElement[] Entries(Relation scope) => Closed(scope, out var items) ? items : throw new ArgumentException("Expected a closed scope.");
    internal static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation u && b is CarrierLocation v && ReferenceEquals(u.Carrier, v.Carrier) && u.SameTickRefAs(v);
    internal static bool Coordinates(IElement[] values, Carrier basis, long[] expected) => values.Length == expected.Length &&
        values.Select((v, i) => v is CarrierLocation at && ReferenceEquals(at.Carrier, basis) && at.Ticks == expected[i]).All(x => x);
    internal static bool Port(Relation port, out Carrier carrier)
    {
        carrier = null!;
        if (port is not { Left: CarrierLocation at, Right: Carrier owner } || owner.IsUnknown ||
            !ReferenceEquals(at.Carrier, owner) || !at.SameTickRefAs(owner.ValueSlot)) return false;
        carrier = owner; return true;
    }
    internal static Relation MakeClamp(Carrier c, long capacity) => new(Relation.Point(c, capacity), Relation.Point(c, capacity));
    internal static bool Clamp(Relation clamp, Carrier c, out long capacity)
    {
        capacity = -1;
        if (clamp is not { Left: Relation a, Right: Relation b } || a.Left is not CarrierLocation x || b.Left is not CarrierLocation y ||
            !ReferenceEquals(a.Right, c) || !ReferenceEquals(b.Right, c) || !ReferenceEquals(x.Carrier, c) ||
            !ReferenceEquals(y.Carrier, c) || x.Ticks != y.Ticks || x.Ticks < 0) return false;
        capacity = x.Ticks; return true;
    }
}
