using Gojo.Core;
using Gojo.Equations;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Experiments.EnoughStructure;

/// <summary>
/// Experimental exact schemas over ordinary Gojo relations. There is no model
/// object or status table: ports, premises, bindings, calculations and history
/// are reachable from the returned relation roots. See README.md for the laws.
/// </summary>
public static class Surfaces
{
    public static Relation Group(params IElement[] parts) => Relation.OrderedGroup(new Carrier(), parts);

    // Port = G(live value, live tension, explicitly exposed native unit frame).
    public static Relation Port(Carrier carrier) =>
        Group(carrier.ValueSlot, carrier.LiveTensionSlot, Relation.Segment(carrier, 0, 1));

    public static bool TryPort(IElement element, out CarrierLocation value,
        out CarrierLocation tension, out Relation frame)
    {
        value = tension = default; frame = null!;
        if (!Fields(element, 3, out var p) || p[0] is not CarrierLocation v ||
            p[1] is not CarrierLocation t || p[2] is not Relation f ||
            !Span(f, out var start, out var end) || start.Carrier.IsUnknown ||
            !v.Carrier.SameIdentityAs(start.Carrier) || !t.Carrier.SameIdentityAs(start.Carrier) ||
            start.Ticks != 0 || end.Ticks != 1) return false;
        value = v; tension = t; frame = f; return true;
    }

    // Application = G(authored pattern, explicit bindings, partial or complete construction).
    public static Relation Bind(IElement pattern, params Relation[] bindings) =>
        Group(pattern, Group(bindings), StructuralPattern.Refine(pattern, bindings));

    public static Relation Direct(IElement construction) => Group(construction, Group(), construction);

    // Quantity question = G(port, affine alignment, declared domain).
    public static Relation AmountQuestion(IElement port, IElement alignment, Relation domain) =>
        Group(port, alignment, domain);

    // Interval question adds its acceptable occupied interval and answer partition.
    // Zero violation occupies one region; positive violation occupies another.
    public static Relation IntervalQuestion(IElement port, IElement alignment,
        Relation domain, Relation acceptable, Relation answerPartition) =>
        Group(port, alignment, domain, acceptable, answerPartition);

    public static Relation CreateDirect(long? capacity = null, UnitShape? unit = null) => Create(capacity, unit, (source, receiver) =>
        Direct(new Relation(Relation.Point(source, 1), Relation.Point(receiver, 0))), Direct);

    public static Relation CreateBound(long? capacity = null, UnitShape? unit = null) => Create(capacity, unit, (source, receiver) =>
    {
        var a = new Placeholder(); var b = new Placeholder();
        return Bind(new Relation(a, b), new Relation(a, Relation.Point(source, 1)),
            new Relation(b, Relation.Point(receiver, 0)));
    }, boundary =>
    {
        var slot = new Placeholder();
        return Bind(new Relation(slot, slot), new Relation(slot, boundary.Left));
    });

    public static Relation CreateOpen(long? capacity = null, UnitShape? unit = null) => Create(capacity, unit, (source, _) =>
    {
        var a = new Placeholder(); var b = new Placeholder();
        return Bind(new Relation(a, b), new Relation(a, Relation.Point(source, 1)));
    }, Direct);

    private static Relation Create(long? capacity, UnitShape? unit, Func<Carrier, Carrier, Relation> route,
        Func<Relation, Relation> boundary)
    {
        var source = unit is null ? new Carrier() : new Carrier(unit); var receiver = new Carrier(source.UnitShape);
        var basis = new Carrier(source.UnitShape); var demand = new Carrier(source.UnitShape);
        var sourcePort = Port(source); var receiverPort = Port(receiver);
        var alignment = new Relation(Relation.Segment(receiver, 0, 1), Relation.Segment(basis, 0, 1));
        var domain = Relation.Segment(basis, 0, 8); var acceptable = Relation.Segment(basis, 3, 8);
        var partition = CoarseReadSurface.Partition(new Carrier(), demand, (0, 0), (1, 8));
        var question = IntervalQuestion(receiverPort, alignment, domain, acceptable, partition);
        var application = route(source, receiver);
        var boundaries = Group();
        if (capacity.HasValue)
        {
            var point = Relation.Point(receiver, capacity.Value);
            var applicationOfBoundary = boundary(new Relation(point, point));
            Fields(applicationOfBoundary, 3, out var parts);
            receiver.Attach((Relation)parts[2]);
            boundaries = Group(applicationOfBoundary);
        }
        // Goal = G(question, available source port, permitted passage, candidate action, desired
        // zero demand, oriented before-minus-after response equation).
        var permitted = new Relation(Relation.Point(source, 1), Relation.Point(receiver, 0));
        var goal = Group(question, sourcePort, permitted, application, new CarrierLocation(demand, 0),
            Equation.Difference(demand, demand, demand));
        // World = G(source port, receiver port, boundary applications, goal, clock).
        return Group(sourcePort, receiverPort, boundaries, goal, new Carrier());
    }

    public static bool TryWorld(Relation world, out Relation sourcePort, out Relation receiverPort,
        out Relation boundaries, out Relation goal, out Carrier clock)
    {
        sourcePort = receiverPort = boundaries = goal = null!; clock = null!;
        if (!Fields(world, 5, out var f) || f[0] is not Relation s || f[1] is not Relation r ||
            f[2] is not Relation b || f[3] is not Relation g || f[4] is not Carrier t ||
            !TryPort(s, out _, out _, out _) || !TryPort(r, out _, out _, out _) ||
            !Fields(g, 6, out _)) return false;
        sourcePort = s; receiverPort = r; boundaries = b; goal = g; clock = t; return true;
    }

    public static Relation Question(Relation world)
    {
        RequireWorld(world, out _, out _, out _, out var goal, out _);
        Fields(goal, 6, out var f); return (Relation)f[0];
    }

    public static Relation QuantityQuestion(Relation world)
    {
        Fields(Question(world), 5, out var q); return AmountQuestion(q[0], q[1], (Relation)q[2]);
    }

    public static Relation Application(Relation world)
    {
        RequireWorld(world, out _, out _, out _, out var goal, out _);
        Fields(goal, 6, out var f); return (Relation)f[3];
    }

    public static Relation ReplaceApplication(Relation world, Relation application)
    {
        RequireWorld(world, out var source, out var receiver, out var boundaries, out var goal, out var clock);
        Fields(goal, 6, out var f);
        return Group(source, receiver, boundaries, Group(f[0], f[1], f[2], application, f[4], f[5]), clock);
    }

    /// <summary>Inspects only the advertised port. All results are captured
    /// observations; the original operational sites stay live and reusable.</summary>
    public static Relation Inspect(Relation question)
    {
        var sampled = Capture(question);
        if (!sampled.TryReadOrderedGroup(out var q) || q.Length is not (3 or 5) ||
            !TryPort(q[0], out var input, out _, out _) || q[1] is not Relation alignment ||
            q[2] is not Relation domain || !Span(domain, out var lower, out var upper) ||
            lower.Carrier.IsUnknown || lower.Ticks > upper.Ticks ||
            !Span(alignment.Left, out var sourceStart, out _) || !Span(alignment.Right, out var targetStart, out _) ||
            !sourceStart.Carrier.SameIdentityAs(input.Carrier) || !targetStart.Carrier.SameIdentityAs(lower.Carrier))
            return Open(sampled, sampled, sampled);

        var projection = alignment.Project(input, lower.Carrier);
        if (!projection.TryReadProjection(out var projected, out var remainder) ||
            !Quiet(remainder) || !projected.Carrier.SameIdentityAs(lower.Carrier))
            return Open(sampled, sampled, Group(projection));

        var domainClaim = OperationClaim.Between(new Carrier(), projected, lower, upper,
            new Carrier(lower.Carrier.UnitShape));
        if (!OperationClaim.TryReadResolvedBetweenViolation(domainClaim, out var domainViolation) ||
            domainViolation.Ticks != 0)
            return Open(sampled, sampled, Group(projection, domainClaim));

        if (q.Length == 3)
            return ReadWitness.Create(new Carrier(), q[0], sampled, projected, remainder,
                Group(projection, domainClaim));

        if (q[3] is not Relation acceptable || !Span(acceptable, out var floor, out var ceiling) ||
            !floor.Carrier.SameIdentityAs(lower.Carrier) || floor.Ticks < lower.Ticks ||
            ceiling.Ticks > upper.Ticks || floor.Ticks > ceiling.Ticks ||
            q[4] is not Relation partition || !CoarseReadSurface.TryReadPartition(partition, out var demand, out _))
            return Open(sampled, sampled, Group(projection, domainClaim));

        var claim = OperationClaim.Between(new Carrier(), projected, floor, ceiling, demand);
        if (!OperationClaim.TryReadResolvedBetweenViolation(claim, out var shortfall) ||
            shortfall.Ticks is < int.MinValue or > int.MaxValue)
            return Open(sampled, sampled, Group(projection, domainClaim, claim));
        var publication = CoarseReadSurface.Measure(new Carrier(), partition, (int)shortfall.Ticks, claim);
        return ReadWitness.Create(new Carrier(), q[0], sampled, publication, remainder,
            Group(projection, domainClaim, claim));
    }

    public static bool TryDemand(Relation witness, out CarrierLocation demand)
    {
        demand = default;
        return ReadWitness.TryRead(witness, out _, out var surface, out var result, out var residue, out var proof) &&
            Fields(surface, 5, out var q) && q[4] is Relation partition &&
            result is Relation publication && CoarseReadSurface.TryReadMask(publication, partition, out var mask) &&
            mask != 0 && Quiet(residue) && Fields(proof, 3, out var calculations) &&
            calculations[2] is Relation claim && OperationClaim.TryReadResolvedBetweenViolation(claim, out demand);
    }

    /// <summary>Returns the occupied answer region, never a boolean invented
    /// from an open comparison or a raw cached violation.</summary>
    public static bool TryAnswer(Relation witness, out IElement answer)
    {
        answer = null!;
        if (!ReadWitness.TryRead(witness, out _, out var surface, out var result, out var residue, out _) ||
            !Quiet(residue) || surface is not Relation question || !question.TryReadOrderedGroup(out var q)) return false;
        if (q.Length == 3 && result is CarrierLocation value) { answer = value; return true; }
        if (q.Length != 5 || !TryDemand(witness, out _) || result is not Relation publication ||
            !ReadWitness.TryRead(publication, out _, out _, out var union, out _, out _) ||
            !Fields(union, 1, out var regions)) return false;
        answer = regions[0]; return true;
    }

    // A read of answer equality uses the two questions' explicit bases and
    // retained correspondences. It asserts this pair of observations only.
    public static Relation Compare(Relation left, Relation right)
    {
        var selected = Group(left, right);
        if (!SameContext(left, right, out var context) || !TryAnswer(left, out var a) || !TryAnswer(right, out var b))
            return Open(selected, selected, selected);
        if (a is CarrierLocation x && b is CarrierLocation y)
        {
            var align = new Relation(Relation.Segment(y.Carrier, 0, 1), Relation.Segment(x.Carrier, 0, 1));
            var mapped = align.Project(y, x.Carrier);
            if (!mapped.TryReadProjection(out var aligned, out var residue) || !Quiet(residue))
                return Open(selected, align, mapped);
            var claim = OperationClaim.Equality(new Carrier(), x, aligned, new Carrier(x.Carrier.UnitShape));
            return ReadWitness.Create(new Carrier(), selected, Group(context, align), claim, residue, mapped);
        }
        if (!Span(a, out var a0, out var a1) || !Span(b, out var b0, out var b1))
            return Open(selected, selected, selected);
        var alignment = new Relation(Relation.Segment(b0.Carrier, 0, 1), Relation.Segment(a0.Carrier, 0, 1));
        var start = alignment.Project(b0, a0.Carrier); var end = alignment.Project(b1, a0.Carrier);
        if (!start.TryReadProjection(out var s, out var sr) || !end.TryReadProjection(out var e, out var er) ||
            !Quiet(sr) || !Quiet(er)) return Open(selected, alignment, Group(start, end));
        var basis = new Carrier(a0.Carrier.UnitShape);
        var first = OperationClaim.Equality(new Carrier(), a0, s, basis);
        var last = OperationClaim.Equality(new Carrier(), a1, e, basis);
        return ReadWitness.Create(new Carrier(), selected, Group(context, alignment), Group(first, last), Group(sr, er), Group(start, end));
    }

    private static bool SameContext(Relation left, Relation right, out Relation context)
    {
        context = Group();
        if (!ReadWitness.TryRead(left, out _, out var ls, out _, out _, out _) ||
            !ReadWitness.TryRead(right, out _, out var rs, out _, out _, out _) ||
            ls is not Relation lq || rs is not Relation rq ||
            !lq.TryReadOrderedGroup(out var l) || !rq.TryReadOrderedGroup(out var r) ||
            l.Length != r.Length || l.Length is not (3 or 5) ||
            !Span(l[2], out var l0, out var l1) || !Span(r[2], out var r0, out var r1) ||
            !l0.Carrier.UnitShape.CompatibleWith(r0.Carrier.UnitShape)) return false;
        // The fixture explicitly selects a native 1:1 metric correspondence;
        // it does not infer shared carrier or observation identity.
        var alignment = new Relation(Relation.Segment(r0.Carrier, 0, 1), Relation.Segment(l0.Carrier, 0, 1));
        var proof = new List<IElement> { alignment };
        bool SameSpan(IElement a, IElement b)
        {
            if (!Span(a, out var a0, out var a1) || !Span(b, out var b0, out var b1)) return false;
            var start = alignment.Project(b0, a0.Carrier); var end = alignment.Project(b1, a0.Carrier);
            proof.Add(start); proof.Add(end);
            return start.TryReadProjection(out var s, out var sr) && end.TryReadProjection(out var e, out var er) &&
                Quiet(sr) && Quiet(er) && s.Equals(a0) && e.Equals(a1);
        }
        if (!SameSpan(l[2], r[2])) return false;
        if (l.Length == 5)
        {
            if (!SameSpan(l[3], r[3]) || l[4] is not Relation lp || r[4] is not Relation rp ||
                !CoarseReadSurface.TryReadPartition(lp, out _, out var lr) ||
                !CoarseReadSurface.TryReadPartition(rp, out _, out var rr) || lr.Length != rr.Length) return false;
            for (var i = 0; i < lr.Length; i++)
            {
                Span(lr[i], out var a0, out var a1); Span(rr[i], out var b0, out var b1);
                if (!a0.Carrier.UnitShape.CompatibleWith(b0.Carrier.UnitShape) || a0.Ticks != b0.Ticks || a1.Ticks != b1.Ticks)
                    return false;
            }
            proof.Add(lp); proof.Add(rp);
        }
        context = Group(proof.ToArray()); return true;
    }

    public static bool TryEquality(Relation witness, out bool equal)
    {
        equal = false;
        if (!ReadWitness.TryRead(witness, out _, out _, out var result, out _, out _)) return false;
        if (result is Relation claim && OperationClaim.TryReadResolvedViolation(claim, out var single))
        { equal = single.Ticks == 0; return true; }
        if (!Fields(result, 2, out var claims) || claims[0] is not Relation first || claims[1] is not Relation last ||
            !OperationClaim.TryReadResolvedViolation(first, out var a) ||
            !OperationClaim.TryReadResolvedViolation(last, out var b)) return false;
        equal = a.Ticks == 0 && b.Ticks == 0; return true;
    }

    public static Relation Activate(Relation world)
    {
        RequireWorld(world, out var sourcePort, out _, out _, out var goal, out _);
        TryPort(sourcePort, out var source, out _, out _);
        var application = Application(world);
        var before = State(world); var beforeQuestion = Inspect(Question(world));
        Fields(goal, 6, out var g);
        var admission = Admit(application, g[2]);
        if (ReadWitness.TryRead(admission, out _, out _, out var result, out _, out _) && result is Relation route)
            source.Carrier.Attach(route);
        return Record(world, before, beforeQuestion, new CarrierLocation(source.Carrier, 0), admission);
    }

    public static Relation Pulse(Relation world, long ticks)
    {
        RequireWorld(world, out var sourcePort, out _, out _, out _, out _);
        TryPort(sourcePort, out var source, out _, out _);
        var before = State(world); var beforeQuestion = Inspect(Question(world));
        var delta = new CarrierLocation(source.Carrier, ticks);
        var execution = Carrier.ReceiveTogether(Group(delta));
        return Record(world, before, beforeQuestion, delta, execution);
    }

    private static Relation Admit(Relation application, IElement requiredSurface)
    {
        if (requiredSurface is not Relation required || !Point(required.Left, out var expectedStart) ||
            !Point(required.Right, out var expectedEnd)) return Open(application, requiredSurface, application);
        if (!Fields(application, 3, out var f) || f[1] is not Relation bindings ||
            !bindings.TryReadOrderedGroup(out var rows) || !UnambiguousBindings(rows) ||
            StructuralPattern.ReadSlots(f[2]).Length != 0 || f[2] is not Relation proposed ||
            !StructuralPattern.Refine(f[0], rows.Cast<Relation>().ToArray()).Equals(proposed) ||
            !Point(proposed.Left, out var start) || !Point(proposed.Right, out var end))
            return Open(application, required, application);
        var residual = new Carrier(expectedStart.Carrier.UnitShape);
        var first = OperationClaim.Equality(new Carrier(), start, expectedStart, residual);
        var last = OperationClaim.Equality(new Carrier(), end, expectedEnd, residual);
        var valid = OperationClaim.TryReadResolvedViolation(first, out var a) && a.Ticks == 0 &&
            OperationClaim.TryReadResolvedViolation(last, out var b) && b.Ticks == 0;
        return ReadWitness.Create(new Carrier(), application, required,
            valid ? proposed : new Placeholder(), valid ? Group() : Group(first, last), Group(first, last));
    }

    private static bool UnambiguousBindings(IElement[] rows)
    {
        for (var i = 0; i < rows.Length; i++)
        {
            if (rows[i] is not Relation binding || !StructuralPattern.TryReadBinding(binding, out var slot, out var value)) return false;
            for (var j = 0; j < i; j++)
            {
                StructuralPattern.TryReadBinding((Relation)rows[j], out var previousSlot, out var previousValue);
                if (slot.Equals(previousSlot) && !ReferenceEquals(value, previousValue)) return false;
            }
        }
        return true;
    }

    public static Relation State(Relation world)
    {
        RequireWorld(world, out var source, out var receiver, out _, out _, out _);
        return Capture(Group(source, receiver));
    }

    private static Relation Record(Relation world, Relation before, Relation beforeQuestion,
        CarrierLocation delta, Relation transition)
    {
        RequireWorld(world, out _, out _, out var boundaries, out var goal, out var clock);
        var after = State(world); var afterQuestion = Inspect(Question(world));
        var flow = Account(before, after, delta, Group(Application(world), boundaries, transition));
        var t = checked((long)clock.Value); clock.Add(1);
        var step = TimeStepSequence.Step(new Carrier(), clock, t, checked(t + 1), before,
            Group(delta, transition, flow), after, Pressure(after));
        Fields(goal, 6, out var g);
        Relation response;
        if (TryDemand(beforeQuestion, out var d0) && TryDemand(afterQuestion, out var d1) &&
            g[4] is CarrierLocation target && g[5] is Relation equation && target.Ticks == 0 &&
            target.Carrier.SameIdentityAs(d0.Carrier) && target.Carrier.SameIdentityAs(d1.Carrier) &&
            equation.Equals(Equation.Difference(target.Carrier, target.Carrier, target.Carrier)))
            response = Equation.Predict(equation, new Relation(d1, d0), target.Carrier);
        else response = Relation.UnresolvedRead(Group(beforeQuestion, afterQuestion), goal, Carrier.Unknown);
        return ReadWitness.Create(new Carrier(), Capture(goal), step,
            Group(beforeQuestion, afterQuestion, response), Group(flow, Pressure(after)), transition);
    }

    public static bool TryResponse(Relation witness, out CarrierLocation beforeDemand,
        out CarrierLocation afterDemand, out CarrierLocation reduction)
    {
        beforeDemand = afterDemand = reduction = default;
        return ReadWitness.TryRead(witness, out _, out _, out var result, out _, out _) &&
            Fields(result, 3, out var f) && f[0] is Relation before && f[1] is Relation after &&
            f[2] is Relation difference && TryDemand(before, out beforeDemand) && TryDemand(after, out afterDemand) &&
            Equation.TryReadPrediction(difference, out reduction, out var remainder) && AllQuiet(remainder);
    }

    // All bookkeeping arithmetic is itself retained as native equations and
    // predictions. Available = accepted + post-source-held; pressure is not
    // duplicated as a second material quantity.
    private static Relation Account(Relation before, Relation after, CarrierLocation delta, IElement route)
    {
        Fields(before, 2, out var b); Fields(after, 2, out var a);
        var bs = Held(b[0], delta.Carrier); var br = Held(b[1], delta.Carrier);
        var nextSource = Held(a[0], delta.Carrier); var nextReceiver = Held(a[1], delta.Carrier);
        var available = Calculate(Equation.Sum(delta.Carrier, Equation.UnitPort(delta.Carrier), Equation.UnitPort(delta.Carrier)),
            new Relation(Value(bs), delta), delta.Carrier);
        var accepted = Calculate(Equation.Difference(delta.Carrier, delta.Carrier, delta.Carrier),
            new Relation(Value(br), Value(nextReceiver)), delta.Carrier);
        var accounted = Calculate(Equation.Sum(delta.Carrier, Equation.UnitPort(delta.Carrier), Equation.UnitPort(delta.Carrier)),
            new Relation(Value(accepted), Value(nextSource)), delta.Carrier);
        var conservation = OperationClaim.Equality(new Carrier(), Value(available), Value(accounted), new Carrier(delta.Carrier.UnitShape));
        return ReadWitness.Create(new Carrier(), Group(before, delta), route,
            Group(Value(available), Value(accepted), a[0], conservation), Group(Pressure(after)),
            Group(bs, br, nextSource, nextReceiver, available, accepted, accounted));
    }

    private static Relation Held(IElement port, Carrier basis)
    {
        TryPort(port, out var value, out var pressure, out _);
        return Calculate(Equation.Sum(basis, Equation.UnitPort(value.Carrier), Equation.UnitPort(value.Carrier)),
            new Relation(value, pressure), basis);
    }

    private static Relation Calculate(Relation equation, IElement input, Carrier basis) =>
        ReadWitness.Create(new Carrier(), input, equation, Equation.Predict(equation, input, basis), Group(), equation);

    private static CarrierLocation Value(Relation calculation)
    {
        ReadWitness.TryRead(calculation, out _, out _, out var result, out _, out _);
        if (result is Relation prediction && Equation.TryReadPrediction(prediction, out var value, out _)) return value;
        throw new InvalidOperationException("The fixture's explicit native accounting equation did not resolve.");
    }

    private static Relation Pressure(Relation state)
    {
        Fields(state, 2, out var ports);
        TryPort(ports[0], out _, out var source, out _); TryPort(ports[1], out _, out var receiver, out _);
        return Group(source, receiver);
    }

    public static bool Fields(IElement element, int count, out IElement[] fields)
    {
        fields = [];
        return element is Relation relation && relation.TryReadOrderedGroup(out fields) && fields.Length == count;
    }

    private static bool Point(IElement element, out CarrierLocation point)
    {
        point = default;
        if (element is not Relation { Left: CarrierLocation location, Right: Carrier carrier } ||
            !location.Carrier.SameIdentityAs(carrier)) return false;
        point = location; return true;
    }

    public static bool Span(IElement element, out CarrierLocation start, out CarrierLocation end)
    {
        start = end = default;
        return element is Relation span && Point(span.Left, out start) && Point(span.Right, out end) &&
            start.Carrier.SameIdentityAs(end.Carrier);
    }

    private static bool Quiet(IElement residue) =>
        ProjectionResidue.TryRead(residue, out _, out var numerator, out _) && numerator == 0;

    private static bool AllQuiet(IElement residue) => Quiet(residue) ||
        residue is Relation pair && AllQuiet(pair.Left) && AllQuiet(pair.Right);

    private static Relation Open(IElement selected, IElement surface, IElement residue) =>
        ReadWitness.Create(new Carrier(), selected, surface, new Placeholder(), residue, Group());

    private static void RequireWorld(Relation world, out Relation source, out Relation receiver,
        out Relation boundaries, out Relation goal, out Carrier clock)
    {
        if (!TryWorld(world, out source, out receiver, out boundaries, out goal, out clock))
            throw new ArgumentException("Expected the exact experimental world surface.", nameof(world));
    }

    /// <summary>Copies only the exposed acyclic surface, keeping carrier
    /// identity and internal site aliases; never traverses private attachments.</summary>
    public static Relation Capture(Relation surface)
    {
        var relations = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var ticks = new Dictionary<TickRef, TickRef>(ReferenceEqualityComparer.Instance);
        IElement Copy(IElement element)
        {
            if (element is CarrierLocation location)
            {
                if (!ticks.TryGetValue(location.Tick, out var tick)) ticks.Add(location.Tick, tick = new TickRef(location.Ticks));
                return new CarrierLocation(location.Carrier, tick);
            }
            if (element is not Relation relation) return element;
            if (relations.TryGetValue(relation, out var previous)) return previous;
            var result = new Relation(Copy(relation.Left), Copy(relation.Right));
            relations.Add(relation, result); return result;
        }
        return (Relation)Copy(surface);
    }
}
