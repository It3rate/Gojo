using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using N = ConnectionAssembly.NativeMath;

namespace ConsumerPrediction;

/// <summary>
/// Supplied experimental scheduling over connected equations. The root carries
/// the operational graph, one outstanding frozen issue, one latest outcome and
/// an admission version. No observer history, candidate label or waveform phase
/// is reachable from this learner.
/// </summary>
public static class PredictionProbe
{
    public static Relation CreateRedundant() => Create((d, _) => d);
    public static Relation CreateScaleRestoration() => Create((_, a) => N.SumTo(N.Output(a), (a, -1)));

    private static Relation Create(Func<Relation, Relation, Relation> buildCandidate)
    {
        var basis = new Carrier();
        var packets = new Carrier();
        var cursor = new CarrierLocation(new Carrier(), 0);
        var sites = Enumerable.Range(0, 3).Select(_ => new Relation(
            new CarrierLocation(basis, 0), new CarrierLocation(packets, 0))).ToArray();
        var present = sites.Select(site => N.Present((CarrierLocation)site.Left, (CarrierLocation)site.Right)).ToArray();
        var a = N.SumTo(basis, (present[1], 1), (present[0], -1));
        var d = N.SumTo(basis, (present[2], 1), (present[1], -1));
        var product = N.Product(a, d);
        var proposedCandidate = buildCandidate(d, a);
        var contribution = N.SumTo(basis, (proposedCandidate, 1), (d, -1));
        // The shadow and actual delivery share the same additive intervention:
        // baseline plus correction, with actual delivery additionally gated.
        var candidate = N.SumTo(basis, (d, 1), (contribution, 1));
        var contributionSite = new CarrierLocation(basis, 0);
        var gate = new Carrier();
        var receipt = new Carrier(basis.UnitShape);
        var publication = Relation.LiveOutput(new Relation(contributionSite, gate.ValueSlot),
            Equation.Product(receipt, Equation.UnitPort(basis), Equation.UnitPort(gate)));
        var receiptTotal = N.SumTo(basis, (receipt.ValueSlot, 1), (receipt.LiveTensionSlot, 1));
        var delivered = N.SumTo(basis, (d, 1), (receiptTotal, 1));
        var support = new Carrier();
        var relief = new CarrierLocation(new Carrier(), 0);
        var calibration = N.Reframe(relief, support);
        var proposed = N.SumTo(support, (support.ValueSlot, 1), (support.LiveTensionSlot, 1), (calibration, 1));
        var supportSurface = G(support, Relation.Segment(support, 0, 8), relief, calibration,
            proposed, BuildTarget(support));
        var demand = new CarrierLocation(packets, 0);
        var tolerance = new CarrierLocation(basis, 0);
        var lowerTolerance = N.SumTo(basis, (tolerance, -1));
        // Retarget only at admission: last level, baseline, candidate, delivered,
        // actual next level. Reads never mutate these sites.
        var outcomeSites = Enumerable.Range(0, 5).Select(_ => new CarrierLocation(basis, 0)).ToArray();
        var actual = N.SumTo(basis, (outcomeSites[4], 1), (outcomeSites[0], -1));
        var errors = outcomeSites.Skip(1).Take(3).Select(forecast =>
            N.SumTo(basis, (forecast, 1), (actual, -1))).ToArray();
        var diagnostics = outcomeSites.Skip(1).Take(3).Select(forecast => G(
            N.Product(forecast, actual), forecast, actual)).ToArray();
        var operational = G(G(sites), G(a, d, product, candidate, contribution),
            G(contributionSite, gate, receipt, publication, receiptTotal, delivered, BuildTarget(gate)),
            supportSurface, G(demand, tolerance, lowerTolerance),
            G(outcomeSites.Cast<IElement>().ToArray()), G(actual, G(errors), G(diagnostics)), cursor,
            N.SumTo(cursor.Carrier, (cursor, 1), (new CarrierLocation(cursor.Carrier, 1), 1)));
        return G(operational, new Placeholder(), new Placeholder(), Copy(cursor));
    }

    // Local surface readers; the groups and bindings remain authoritative.
    public static IElement[] Parts(Relation group) => N.Parts(group);
    public static Relation Operational(Relation root) => (Relation)Parts(root)[0];
    public static Relation? Pending(Relation root) => Parts(root)[1] as Relation;
    public static Relation? LatestOutcome(Relation root) => Parts(root)[2] as Relation;
    public static Relation[] Equations(Relation root) => Parts((Relation)Op(root)[1]).Cast<Relation>().ToArray();
    public static Carrier Support(Relation root) => (Carrier)Parts((Relation)Op(root)[3])[0];
    public static Carrier Gate(Relation root) => (Carrier)Route(root)[1];
    public static Carrier Receipt(Relation root) => (Carrier)Route(root)[2];
    public static Relation Publication(Relation root) => (Relation)Route(root)[3];
    public static bool Attached(Relation root) => Receipt(root).IsAttached(Publication(root));
    public static long Version(Relation root) => ((CarrierLocation)Op(root)[7]).Ticks;
    public static Relation InputSites(Relation root) => (Relation)Op(root)[0];

    /// <summary>Pure current read. Input presence is a native affine span: no
    /// current sample is distinct from a valid sample whose coordinate is zero.</summary>
    public static Relation Forecast(Relation root)
    {
        var reads = N.ReadMany(Equations(root).Cast<IElement>().ToArray());
        IElement turn = Relation.UnresolvedRead(reads[2], Equations(root)[2], N.Output(Equations(root)[2]));
        if (Scalar(reads[2], out var product))
            turn = OperationClaim.AtMost(new Carrier(), product, new CarrierLocation(product.Carrier, -1), new Carrier());
        return G(G(reads), turn);
    }

    public static bool ForecastReady(Relation forecast) => Parts((Relation)Parts(forecast)[0])
        .Cast<Relation>().All(read => Scalar(read, out _));
    public static bool ForecastEligible(Relation forecast) => ForecastReady(forecast) &&
        Parts(forecast)[1] is Relation turn && Pass(turn);

    /// <summary>Closes prior delivery, evaluates precisely its immediate next
    /// sample, admits feedback, then issues a new prediction. Demand/tolerance
    /// arguments apply only to the new issue; old premises are frozen.</summary>
    public static bool TryObserve(Relation root, long? input, bool demand, long tolerance,
        out Relation next, out Relation outcome)
    {
        next = root;
        outcome = Relation.UnresolvedRead(root, root, Carrier.Unknown);
        var op = Op(root);
        var cursor = (CarrierLocation)op[7];
        if (!Pass(OperationClaim.Equality(new Carrier(), Parts(root)[3], Copy(cursor), new Carrier()))) return false;
        if (tolerance < 0) return false; // Invalid host-supplied experimental contract, not failed inference.
        var nextCursor = Value(N.Read((Relation)op[8]));
        ClosePulse(root);
        outcome = Evaluate(root, input, nextCursor);
        var sites = Parts((Relation)op[0]).Cast<Relation>().ToArray();
        if (input.HasValue)
        {
            for (var i = 0; i < 2; i++)
            {
                ((CarrierLocation)sites[i].Left).Tick.Set(((CarrierLocation)sites[i + 1].Left).Ticks);
                ((CarrierLocation)sites[i].Right).Tick.Set(((CarrierLocation)sites[i + 1].Right).Ticks);
            }
            ((CarrierLocation)sites[2].Left).Tick.Set(input.Value);
            ((CarrierLocation)sites[2].Right).Tick.Set(1);
        }
        else foreach (var site in sites) ((CarrierLocation)site.Right).Tick.Set(0);
        cursor.Tick.Set(nextCursor.Ticks);
        var consumer = Parts((Relation)op[4]);
        ((CarrierLocation)consumer[0]).Tick.Set(demand ? 1 : 0);
        ((CarrierLocation)consumer[1]).Tick.Set(tolerance);
        var forecast = Forecast(root);
        var ready = ForecastReady(forecast);
        var eligible = ForecastEligible(forecast);
        var route = Route(root);
        var participate = eligible && demand && Attached(root);
        if (participate)
        {
            ((CarrierLocation)route[0]).Tick.Set(Value((Relation)Parts((Relation)Parts(forecast)[0])[4]).Ticks);
            SetTarget((Relation)route[6], new CarrierLocation(Gate(root), 1));
        }
        var delivered = ready ? (IElement)Copy(Value(N.Read((Relation)route[5]))) : new Placeholder();
        var lower = Value(N.Read((Relation)consumer[2]));
        var interval = Relation.Segment(lower.Carrier, lower.Ticks, ((CarrierLocation)consumer[1]).Ticks);
        var premises = G(sites.Select(site => (IElement)new Relation(Copy((CarrierLocation)site.Left),
            Copy((CarrierLocation)site.Right))).ToArray());
        // Issue10: version, frozen current level/open, pure forecast, frozen
        // demand, interval, actual delivered forecast, route participation,
        // support at issue, frozen three-level premises, actual contribution.
        var pending = G(Copy(cursor), input.HasValue ? Copy((CarrierLocation)sites[2].Left) : new Placeholder(),
            forecast, Copy((CarrierLocation)consumer[0]), interval, delivered,
            new CarrierLocation(Gate(root), participate ? 1 : 0), Copy(Support(root).ValueSlot),
            premises, Copy(Value(N.Read((Relation)route[4]))));
        next = G(Operational(root), pending, outcome, Copy(cursor));
        return true;
    }

    private static Relation Evaluate(Relation root, long? input, CarrierLocation outcomeTime)
    {
        var pending = Pending(root);
        if (pending is null) return G(new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder(),
            new Placeholder(), Copy(Support(root).ValueSlot), Copy(Support(root).ValueSlot));
        var issue = Parts(pending);
        var forecast = (Relation)issue[2];
        var before = Copy(Support(root).ValueSlot);
        var metric = ((CarrierLocation)Parts((Relation)Op(root)[4])[1]).Carrier;
        IElement sample = input.HasValue ? new CarrierLocation(metric, input.Value) : new Placeholder();
        if (!ForecastReady(forecast) || !input.HasValue)
        {
            var unresolved = Relation.UnresolvedRead(sample, (Relation)issue[4], metric);
            var openStep = TimeStepSequence.Step(new Carrier(), outcomeTime.Carrier,
                ((CarrierLocation)issue[0]).Ticks, outcomeTime.Ticks, pending,
                (Relation)issue[4], sample, unresolved);
            return G(pending, sample, unresolved, new Placeholder(),
                G(new Placeholder(), new Placeholder(), new Placeholder(), new Placeholder(), openStep), before, before);
        }
        var reads = Parts((Relation)Parts(forecast)[0]).Cast<Relation>().ToArray();
        var coordinates = Parts((Relation)Op(root)[5]).Cast<CarrierLocation>().ToArray();
        var desired = new[] { (CarrierLocation)issue[1], Value(reads[1]), Value(reads[3]),
            (CarrierLocation)issue[5], (CarrierLocation)sample };
        for (var i = 0; i < coordinates.Length; i++) coordinates[i].Tick.Set(desired[i].Ticks);
        var outcomeEquations = Parts((Relation)Op(root)[6]);
        var actual = Value(N.Read((Relation)outcomeEquations[0]));
        var errors = N.ReadMany(Parts((Relation)outcomeEquations[1]));
        var responseBasis = ((CarrierLocation)Parts((Relation)Op(root)[3])[2]).Carrier;
        var transition = G(Copy(Value(reads[1])), Copy(Value(reads[4])), Copy(Value(reads[3])),
            Equations(root)[4], Publication(root));
        var shadow = OperationClaim.IntervalResponse(new Carrier(), (Relation)issue[4],
            Value(errors[0]), Value(errors[1]), responseBasis, transition);
        var delivered = OperationClaim.IntervalResponse(new Carrier(), (Relation)issue[4],
            Value(errors[0]), Value(errors[2]), new Carrier(responseBasis.UnitShape),
            G(Copy(Value(reads[1])), issue[9], issue[5], Publication(root)));
        var diagnostics = Parts((Relation)outcomeEquations[2]).Cast<Relation>()
            .Select(surface => (IElement)DirectionAndScale(surface)).ToArray();
        IElement feedback = new Placeholder();
        if (ForecastEligible(forecast) && ((CarrierLocation)issue[3]).Ticks == 1 &&
            OperationClaim.TryReadIntervalResponse(shadow, out _, out _, out _, out _, out _, out var relief))
            feedback = Reinforce(root, relief);
        // Outcome7: issue, observed level, shadow response, delivered response,
        // native actual/errors/diagnostics/feedback, support before and after.
        var timeline = TimeStepSequence.Step(new Carrier(), outcomeTime.Carrier,
            ((CarrierLocation)issue[0]).Ticks, outcomeTime.Ticks, pending, transition,
            G(sample, shadow, delivered), G(Value(errors[0]), Value(errors[1]), Value(errors[2])));
        return G(pending, sample, shadow, delivered, G(Copy(actual), G(errors), G(diagnostics), feedback, timeline),
            before, Copy(Support(root).ValueSlot));
    }

    private static Relation DirectionAndScale(Relation surface)
    {
        var p = Parts(surface);
        var forecast = Copy((CarrierLocation)p[1]);
        var actual = Copy(Value(N.Read((Relation)p[2])));
        var product = Value(N.Read((Relation)p[0]));
        var aligned = OperationClaim.AtLeast(new Carrier(), product, new CarrierLocation(product.Carrier, 1), new Carrier());
        var zeroForecast = OperationClaim.Equality(new Carrier(), forecast, new CarrierLocation(forecast.Carrier, 0), actual.Carrier);
        var zeroActual = OperationClaim.Equality(new Carrier(), actual, new CarrierLocation(actual.Carrier, 0), actual.Carrier);
        // Equality against zero exposes native absolute magnitudes on one basis.
        if (!OperationClaim.TryReadResolvedViolation(zeroForecast, out var forecastMagnitude) ||
            !OperationClaim.TryReadResolvedViolation(zeroActual, out var actualMagnitude))
            return G(surface, aligned, zeroForecast, zeroActual, new Placeholder());
        var scale = OperationClaim.Equality(new Carrier(), forecastMagnitude, actualMagnitude, new Carrier());
        return G(surface, aligned, zeroForecast, zeroActual, scale);
    }

    public static bool DirectionAgrees(Relation diagnostics)
    {
        var p = Parts(diagnostics);
        return Pass((Relation)p[1]) || Pass((Relation)p[2]) && Pass((Relation)p[3]);
    }
    public static long ScaleError(Relation diagnostics) =>
        OperationClaim.TryReadResolvedViolation((Relation)Parts(diagnostics)[4], out var magnitude)
            ? magnitude.Ticks : throw new InvalidOperationException("Unresolved native scale diagnostic.");

    private static Relation Reinforce(Relation root, CarrierLocation relief)
    {
        var p = Parts((Relation)Op(root)[3]);
        ((CarrierLocation)p[2]).Tick.Set(relief.Ticks);
        var calibration = N.Read((Relation)p[3]);
        var proposed = N.Read((Relation)p[4]);
        var candidate = Value(proposed);
        if (!TimeStepSequence.TryReadSpan((Relation)p[1], out var lower, out var upper))
            throw new InvalidOperationException("Missing native support interval.");
        var lowClaim = OperationClaim.AtLeast(new Carrier(), candidate, lower, new Carrier());
        var highClaim = OperationClaim.AtMost(new Carrier(), candidate, upper, new Carrier());
        var bounded = !Pass(lowClaim) ? lower : !Pass(highClaim) ? upper : candidate;
        var admitted = SetTarget((Relation)p[5], bounded);
        var enabled = OperationClaim.AtLeast(new Carrier(), Copy(Support(root).ValueSlot),
            new CarrierLocation(Support(root), 1), new Carrier());
        if (Pass(enabled))
        {
            if (!Attached(root)) Receipt(root).Attach(Publication(root));
        }
        else if (Attached(root)) Receipt(root).Detach(Publication(root));
        return G(Copy(relief), calibration, proposed, lowClaim, highClaim, admitted, enabled);
    }

    private static void ClosePulse(Relation root)
    {
        var route = Route(root);
        // Attached output first returns to zero. Withdrawal itself does not
        // retract a delivered signed level or its pressure.
        SetTarget((Relation)route[6], new CarrierLocation(Gate(root), 0));
        ((CarrierLocation)route[0]).Tick.Set(0);
        if (Attached(root)) Receipt(root).Settle();
    }

    private static Relation BuildTarget(Carrier carrier)
    {
        var target = new CarrierLocation(carrier, 0);
        return G(carrier, target, N.SumTo(carrier, (target, 1),
            (carrier.ValueSlot, -1), (carrier.LiveTensionSlot, -1)));
    }
    private static Relation SetTarget(Relation targetSurface, CarrierLocation target)
    {
        var p = Parts(targetSurface);
        ((CarrierLocation)p[1]).Tick.Set(target.Ticks);
        var delta = N.Read((Relation)p[2]);
        var settlement = Carrier.ReceiveTogether(G(Value(delta)));
        return G(Copy(target), delta, settlement);
    }
    private static IElement[] Op(Relation root) => Parts(Operational(root));
    private static IElement[] Route(Relation root) => Parts((Relation)Op(root)[2]);
    public static bool Pass(Relation claim) => OperationClaim.TryReadResolvedViolation(claim, out var v) && v.Ticks == 0;
    public static bool Scalar(Relation read, out CarrierLocation value) => Equation.TryReadPrediction(read, out value, out _);
    public static CarrierLocation Value(Relation read) => Scalar(read, out var value) ? value :
        throw new InvalidOperationException("The supplied experiment expected a resolved native arithmetic read.");
    private static CarrierLocation Copy(CarrierLocation location) => new(location.Carrier, location.Ticks);
    private static Relation G(params IElement[] parts) => N.Group(parts);
}
