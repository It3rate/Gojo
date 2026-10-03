using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using N = BreakDrivenPrediction.NativeCircuit;

namespace BreakDrivenPrediction;

/// <summary>
/// Exploratory construction and local scheduling policy, not a semantic object.
/// All retained state and dependencies are reachable from the returned relation.
/// See PROTOCOL.md for the supplied policy and its deliberately narrow scope.
/// </summary>
public static class Continuation
{
    // Root: sensor, clock, candidates, selected binding, run start, onset,
    // proposals, pressure, policy, latest event, participating timing bindings.
    public static Relation Create(long initial, long requiredSupport = 2)
    {
        var sensor = new Carrier();
        var clock = new Carrier();
        Carrier.ReceiveTogether(G(new CarrierLocation(sensor, initial)));
        Relation Candidate(long delta)
        {
            var step = new CarrierLocation(sensor, delta);
            var level = N.Sum((sensor.ValueSlot, 1), (sensor.LiveTensionSlot, 1));
            var recurrence = N.Reframe(N.Sum((level, 1), (step, 1)), sensor);
            return G(step, recurrence, new Relation(new Placeholder(), step));
        }
        var candidates = G(Candidate(1), Candidate(-1));
        var pressure = new Carrier();
        // A one-tick reset keeps no visible integer value; every whole request
        // remains on its unresolved pressure channel until explicitly handled.
        pressure.Attach(new Relation(Relation.Point(pressure, 1), Relation.Point(pressure, 0)));
        // Policy: participation floor, support ceiling, reward, draw.
        var policy = G(N.Constant(requiredSupport), N.Constant(4), N.Constant(1), N.Constant(-1));
        return G(sensor, clock, candidates, new Relation(new Placeholder(), Parts(candidates)[0]),
            new CarrierLocation(clock, 0), new Placeholder(), G(), pressure, policy,
            new Placeholder(), G());
    }

    public static IElement[] Parts(Relation group) =>
        group.TryReadOrderedGroup(out var parts) ? parts : [];
    public static Relation G(params IElement[] parts) => N.Group(parts);
    public static Carrier Sensor(Relation root) => (Carrier)Parts(root)[0];
    public static Carrier Clock(Relation root) => (Carrier)Parts(root)[1];
    public static Carrier Pressure(Relation root) => (Carrier)Parts(root)[7];
    public static Relation Selected(Relation root) => (Relation)((Relation)Parts(root)[3]).Right;
    public static CarrierLocation Step(Relation candidate) => (CarrierLocation)Parts(candidate)[0];
    public static Relation[] Proposals(Relation root) => Parts((Relation)Parts(root)[6]).Cast<Relation>().ToArray();
    public static Relation[] Connections(Relation root) => Parts((Relation)Parts(root)[10]).Cast<Relation>().ToArray();

    // Issue: clock, issued-at, due-at, input level, selected binding,
    // frozen baseline, frozen export, proposal/open, frozen shadow/open,
    // due claim/open, support claim/open.
    public static Relation Issue(Relation root)
    {
        try { return IssueCore(root); }
        catch (OverflowException)
        {
            // The full requested structure remains inspectable; no saturated
            // scalar forecast is manufactured at the representable boundary.
            return Relation.UnresolvedRead(root, root, SensorOrUnknown(root));
        }
    }

    private static Relation IssueCore(Relation root)
    {
        if (!TryRoot(root, out var p)) return Relation.UnresolvedRead(root, root, Carrier.Unknown);
        var clock = (Carrier)p[1];
        var now = Copy(clock.ValueSlot);
        var selected = (Relation)p[3];
        var candidate = (Relation)selected.Right;
        var level = Level((Carrier)p[0]);
        var nativeRead = N.Read(Parts(candidate)[1]);
        if (!Equation.TryReadPrediction(nativeRead, out var nextLevel, out _)) return nativeRead;
        var baseline = Difference(nextLevel, level, (Carrier)p[0]);
        var dueAt = Calculate(N.Reframe(N.Sum((now, 1), (N.Constant(1), 1)), clock));
        IElement proposal = new Placeholder(), shadow = new Placeholder();
        IElement dueClaim = new Placeholder(), supportClaim = new Placeholder();
        var exported = baseline;
        foreach (var edge in Proposals(root))
        {
            if (p[5] is not Relation) break; // No observed onset, no usable phase.
            var e = Parts(edge);
            if (e[0] is not Relation route || !ReferenceEquals(route.Left, candidate)) continue;
            var remaining = Calculate(e[3]);
            var claim = OperationClaim.Equality(new Carrier(), remaining,
                new CarrierLocation(remaining.Carrier, 0), new Carrier());
            dueClaim = claim;
            if (!Pass(claim)) continue;
            proposal = edge;
            shadow = Copy(Step((Relation)route.Right));
            var surplus = Calculate(e[4]);
            var support = OperationClaim.AtLeast(new Carrier(), surplus,
                new CarrierLocation(surplus.Carrier, 0), new Carrier());
            supportClaim = support;
            // The membership edge, not just a scalar score, authorizes use.
            if (Pass(support) && Connections(root).Any(link => ReferenceEquals(link.Right, edge)))
                exported = (CarrierLocation)shadow;
            break;
        }
        return G(clock, now, dueAt, level, selected, baseline, exported,
            proposal, shadow, dueClaim, supportClaim);
    }

    /// <summary>
    /// Admit and assess exactly the next observation, then adapt. Repeating an
    /// issue, using another stream's issue, or supplying an incompatible input
    /// leaves the world untouched and returns nonnumeric unresolved evidence.
    /// </summary>
    public static bool TryObserve(Relation root, Relation issue, CarrierLocation observation,
        out Relation next, out Relation assessment)
    {
        // Range failures in assessment occur before admission. The small bounded
        // policy/trace calculations after admission fit once dueAt and delta do.
        try { return ObserveCore(root, issue, observation, out next, out assessment); }
        catch (OverflowException)
        {
            next = root;
            assessment = Relation.UnresolvedRead(observation, issue, SensorOrUnknown(root));
            return false;
        }
    }

    private static bool ObserveCore(Relation root, Relation issue, CarrierLocation observation,
        out Relation next, out Relation assessment)
    {
        next = root;
        assessment = Relation.UnresolvedRead(observation, issue, SensorOrUnknown(root));
        if (!TryRoot(root, out var p) || Parts(issue) is not { Length: 11 } f ||
            f[0] is not Carrier sourceClock || !ReferenceEquals(sourceClock, p[1]) ||
            f[1] is not CarrierLocation issuedAt || f[2] is not CarrierLocation dueAt ||
            f[3] is not CarrierLocation prior || f[4] is not Relation selection ||
            !ReferenceEquals(selection, p[3]) || f[5] is not CarrierLocation baseline ||
            f[6] is not CarrierLocation exported || observation.Tick is null ||
            !ReferenceEquals(observation.Carrier, p[0])) return false;
        var sensor = (Carrier)p[0];
        var clock = (Carrier)p[1];
        if (!ReferenceEquals(baseline.Carrier, sensor) || !ReferenceEquals(exported.Carrier, sensor) ||
            (f[8] is CarrierLocation shadowBasis && !ReferenceEquals(shadowBasis.Carrier, sensor))) return false;
        if (!Pass(OperationClaim.Equality(new Carrier(), issuedAt, Copy(clock.ValueSlot), new Carrier())) ||
            !Pass(OperationClaim.Equality(new Carrier(), prior, Level(sensor), new Carrier())) ||
            !Pass(OperationClaim.Equality(new Carrier(), dueAt,
                Calculate(N.Reframe(N.Sum((clock.ValueSlot, 1), (N.Constant(1), 1)), clock)), new Carrier())))
            return false;

        var observed = Copy(observation);
        var actual = Difference(observed, prior, sensor);
        var baselineClaim = Agreement(baseline, actual);
        var exportClaim = Agreement(exported, actual);
        var magnitudeClaim = OperationClaim.Equality(new Carrier(), Magnitude(exported), Magnitude(actual), new Carrier());
        var shadowClaim = f[8] is CarrierLocation shadow ? Agreement(shadow, actual) : null;
        if (!Resolved(baselineClaim) || !Resolved(exportClaim) ||
            (shadowClaim is not null && !Resolved(shadowClaim))) return false;
        // All claims above are frozen before either sensor admission or learning.
        var pressure = (Carrier)p[7];
        var pressureBefore = Copy(pressure.LiveTensionSlot);
        var pressureClaim = OperationClaim.AtLeast(new Carrier(), pressureBefore,
            new CarrierLocation(pressure, 1), new Carrier());
        var admission = Carrier.ReceiveTogether(G(Difference(observed, Level(sensor), sensor),
            new CarrierLocation(clock, 1)));
        if (!SettlementObservation.ObservedQuiet(admission))
        {
            assessment = G(assessment, admission);
            return false;
        }

        var edges = Proposals(root).ToList();
        var updates = new List<IElement>();
        var policy = Parts((Relation)p[8]);
        if (f[7] is Relation tested && shadowClaim is not null && edges.Any(e => ReferenceEquals(e, tested)))
        {
            var credit = (Carrier)Parts(tested)[2];
            IElement? response = null;
            if (Pass(shadowClaim) && !Pass(baselineClaim)) response = policy[2];
            if (!Pass(shadowClaim) && Pass(baselineClaim)) response = policy[3];
            if (response is not null)
            {
                var before = Copy(credit.ValueSlot);
                var delta = Calculate(N.Reframe(response, credit));
                var proposed = Calculate(N.Reframe(N.Sum((before, 1), (delta, 1)), credit));
                var lower = OperationClaim.AtLeast(new Carrier(), proposed, new CarrierLocation(credit, 0), new Carrier());
                var upper = OperationClaim.AtMost(new Carrier(), proposed,
                    Calculate(N.Reframe(policy[1], credit)), new Carrier());
                // Saturation is bounded forgetting: an inapplicable reward/draw
                // remains a rejected response, never accumulating hidden debt.
                IElement received = Pass(lower) && Pass(upper)
                    ? Carrier.ReceiveTogether(G(delta)) : G(lower, upper);
                updates.Add(G(before, delta, Copy(credit.ValueSlot), Copy(credit.LiveTensionSlot), received));
            }
        }

        var candidate = (Relation)selection.Right;
        var newSelection = selection;
        IElement onset = p[5];
        var runStart = (CarrierLocation)p[4];
        var search = new List<IElement>();
        var localBreak = !Pass(baselineClaim);
        if (localBreak)
        {
            Relation? recruited = null;
            foreach (var possible in Parts((Relation)p[2]).Cast<Relation>())
            {
                var match = Agreement(Copy(Step(possible)), actual);
                search.Add(G(possible, match));
                if (Pass(match)) { recruited = possible; break; }
            }
            if (recruited is not null)
            {
                // Only an observed onset justifies a complete duration.
                if (onset is Relation)
                {
                    var span = new Relation(new Relation(Copy(runStart), clock), new Relation(Copy(issuedAt), clock));
                    var existing = edges.FirstOrDefault(e => Parts(e)[0] is Relation route &&
                        ReferenceEquals(route.Left, candidate) && ReferenceEquals(route.Right, recruited));
                    var credit = existing is null ? NewCredit(policy) : (Carrier)Parts(existing)[2];
                    var replace = existing is null;
                    if (existing is not null)
                    {
                        var sameLength = OperationClaim.Equality(new Carrier(), Length(span),
                            Length((Relation)Parts(existing)[1]), new Carrier());
                        if (!Pass(sameLength))
                        {
                            // Evidence supported the old duration. It cannot transfer for free.
                            updates.Add(G(sameLength, Copy(credit.ValueSlot), Copy(credit.LiveTensionSlot)));
                            credit = NewCredit(policy);
                            replace = true;
                        }
                    }
                    if (replace)
                    {
                        if (existing is not null) edges.Remove(existing);
                        var length = N.Sum((((Relation)span.Right).Left, 1), (((Relation)span.Left).Left, -1));
                        var remaining = N.Sum((clock.ValueSlot, 1), (runStart, -1), (length, -1));
                        var surplus = N.Sum((credit.ValueSlot, 1), (policy[0], -1));
                        edges.Add(G(new Relation(candidate, recruited), span, credit, remaining, surplus));
                    }
                }
                updates.Add(Retarget(Step(recruited), actual));
                newSelection = new Relation(selection.Left, recruited);
                updates.Add(Retarget(runStart, issuedAt));
                onset = G(Copy(issuedAt), Copy(dueAt), baseline, actual);
            }
            else
            {
                // No zero/flat primitive was supplied. A gap invalidates the run;
                // it must not be silently included in a later complete duration.
                onset = new Placeholder();
                updates.Add(Retarget(runStart, dueAt));
            }
        }
        else if (Pass(pressureClaim))
        {
            // A supplied downstream request may refine scale during directional success.
            updates.Add(Retarget(Step(candidate), actual));
        }

        // Inspection requests are consumed once only when a readable response exists.
        if (Pass(pressureClaim) && (Pass(baselineClaim) || !ReferenceEquals(selection, newSelection)))
        {
            var consumed = Calculate(N.Reframe(N.Sum((pressureBefore, -1)), pressure));
            updates.Add(Carrier.ReceiveTogether(G(consumed)));
        }
        var links = new List<IElement>();
        foreach (var edge in edges)
        {
            var credit = (Carrier)Parts(edge)[2];
            var floor = Calculate(N.Reframe(policy[0], credit));
            var qualifies = OperationClaim.AtLeast(new Carrier(), Copy(credit.ValueSlot), floor, new Carrier());
            if (Pass(qualifies))
            {
                var old = Connections(root).FirstOrDefault(link => ReferenceEquals(link.Right, edge));
                links.Add(old ?? new Relation(Parts(edge)[0], edge));
            }
        }
        // Return rich evidence to the observer, but retain only the latest bounded
        // event. Never link this event to the previous root or previous event.
        assessment = G(issuedAt, dueAt, baseline, exported, actual, baselineClaim, exportClaim,
            magnitudeClaim, (IElement?)shadowClaim ?? new Placeholder(), G(search.ToArray()),
            G(updates.ToArray()), pressureBefore, Copy(pressure.LiveTensionSlot));
        var meaningful = localBreak || shadowClaim is not null || Pass(pressureClaim);
        var lastEvent = meaningful
            ? TimeStepSequence.Step(new Carrier(), clock, issuedAt.Ticks, dueAt.Ticks,
                G(baseline, exported), G(actual, baselineClaim, exportClaim,
                    magnitudeClaim, (IElement?)shadowClaim ?? new Placeholder()),
                G(Copy(Step((Relation)newSelection.Right)), Copy(runStart)),
                G(pressureBefore, Copy(pressure.LiveTensionSlot)))
            : p[9];
        next = G(sensor, clock, p[2], newSelection, runStart, onset, G(edges.ToArray()),
            pressure, p[8], lastEvent, G(links.ToArray()));
        return true;
    }

    public static Relation Agreement(CarrierLocation prediction, CarrierLocation actual)
    {
        // Explicit common sensor basis; unrelated sensors remain unresolved.
        if (!prediction.Carrier.SameIdentityAs(actual.Carrier))
            return OperationClaim.Equality(new Carrier(), prediction, actual, new Carrier());
        var circuit = N.Product(prediction, actual);
        var read = N.Read(circuit);
        if (!Equation.TryReadPrediction(read, out var product, out _)) return read;
        var claim = OperationClaim.AtLeast(new Carrier(), product,
            new CarrierLocation(product.Carrier, 1), new Carrier());
        return G(claim, new Relation(circuit, read));
    }

    public static bool Pass(Relation claim) =>
        OperationClaim.TryReadResolvedViolation(Claim(claim), out var amount) && amount.Ticks == 0;

    private static bool Resolved(Relation claim) =>
        OperationClaim.TryReadResolvedViolation(Claim(claim), out _);
    private static Relation Claim(Relation evidence) => Parts(evidence) is { Length: 2 } p &&
        p[0] is Relation claim && p[1] is Relation ? claim : evidence;

    public static CarrierLocation Magnitude(CarrierLocation value)
    {
        var claim = OperationClaim.Equality(new Carrier(), value, new CarrierLocation(value.Carrier, 0), value.Carrier);
        if (!OperationClaim.TryReadResolvedViolation(claim, out var magnitude))
            throw new InvalidOperationException("Probe requires a complete native magnitude read.");
        return magnitude;
    }

    public static CarrierLocation Difference(CarrierLocation a, CarrierLocation b, Carrier target) =>
        Calculate(N.Reframe(N.Sum((a, 1), (b, -1)), target));
    public static CarrierLocation Level(Carrier carrier) =>
        Calculate(N.Reframe(N.Sum((carrier.ValueSlot, 1), (carrier.LiveTensionSlot, 1)), carrier));
    public static CarrierLocation Copy(CarrierLocation value) => new(value.Carrier, value.Ticks);
    public static CarrierLocation Length(Relation span)
    {
        if (!TimeStepSequence.TryReadSpan(span, out var start, out var end))
            throw new InvalidOperationException("A timing proposal requires an ordered run span.");
        return Difference(end, start, start.Carrier);
    }
    private static CarrierLocation Calculate(IElement circuit) => N.Value(circuit);
    private static Carrier SensorOrUnknown(Relation root) =>
        TryRoot(root, out var p) ? (Carrier)p[0] : Carrier.Unknown;
    private static bool TryRoot(Relation root, out IElement[] p)
    {
        p = Parts(root);
        return p.Length == 11 && p[0] is Carrier && p[1] is Carrier && p[2] is Relation &&
            p[3] is Relation { Right: Relation } && p[4] is CarrierLocation &&
            p[6] is Relation && p[7] is Carrier && p[8] is Relation && p[10] is Relation;
    }
    private static Carrier NewCredit(IElement[] policy)
    {
        var credit = new Carrier();
        var ceiling = Calculate(N.Reframe(policy[1], credit));
        credit.Attach(new Relation(new Relation(ceiling, credit), new Relation(Copy(ceiling), credit)));
        return credit;
    }
    private static Relation Retarget(CarrierLocation target, CarrierLocation source)
    {
        var before = Copy(target);
        var map = N.Reframe(source, target.Carrier);
        var calculated = Calculate(map);
        // This is explicit structural memory, not direct carrier value input.
        // The native map and target connection remain in the response witness.
        if (!N.Assign(target, calculated)) return Relation.UnresolvedRead(source, map, target.Carrier);
        return G(before, new Relation(map, target), Copy(target));
    }
}
